using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace HRPermissionManagement.Controllers
{
    public class AccountController(AppDbContext context) : Controller
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Girilen şifreyi (SHA256) Hash'leyip arıyoruz
            string hashedPassword = HRPermissionManagement.Helpers.Hasher.HashPassword(password);

            var user = _context.Employees.FirstOrDefault(x => x.Email == email && x.Password == hashedPassword);

            if (user != null)
            {
                // YENİ EKLENEN KISIM: Kullanıcının bir departman yöneticisi olup olmadığını kontrol et
                bool isManager = _context.Departments.Any(d => d.ManagerId == user.Id);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.Name),
                    new(ClaimTypes.Email, user.Email),
                    new("EmployeeId", user.Id.ToString()),
                    new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
                };

                // Eğer kullanıcı bir departman yöneticisiyse bu Claim'i ekle
                if (isManager)
                {
                    claims.Add(new Claim("IsManager", "true"));
                }

                var userIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(userIdentity);

                await HttpContext.SignInAsync("CookieAuth", principal);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Lütfen e-posta adresinizi giriniz.";
                return View();
            }

            var user = _context.Employees.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Bu e-posta adresi ile kayıtlı çalışan bulunamadı.";
                return View();
            }

            try
            {
                // 1. Rastgele yeni şifre oluştur (6 haneli sayı)
                Random rnd = new();
                string yeniSifre = rnd.Next(100000, 999999).ToString();

                // 2. Veritabanına şifreyi SHA256 olarak kaydet
                user.Password = HRPermissionManagement.Helpers.Hasher.HashPassword(yeniSifre);
                _context.SaveChanges();

                // 3. MAİL GÖNDERME AYARLARI (GMAIL ÖRNEĞİ)
                // -----------------------------------------------------
                string gonderenMail = "iderberke@gmail.com"; // BURAYI DOLDUR
                string gonderenSifre = "endd cyqs clnv onqc"; // BURAYI DOLDUR
                string smtpHost = "smtp.gmail.com";
                int smtpPort = 587; // Gmail portu

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.EnableSsl = true; // Gmail için SSL şart
                    smtp.Credentials = new NetworkCredential(gonderenMail, gonderenSifre);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(gonderenMail, "HR Yönetim Sistemi")
                    };

                    // Kime gidecek?
                    mailMessage.To.Add(user.Email);

                    mailMessage.Subject = "Şifre Sıfırlama Bilgilendirmesi";
                    mailMessage.Body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd;'>
                    <h2>Merhaba {user.Name},</h2>
                    <p>Talebiniz üzerine şifreniz sıfırlandı.</p>
                    <p><strong>Yeni Şifreniz:</strong> <span style='font-size: 18px; color: #d9534f;'>{yeniSifre}</span></p>
                    <p>Lütfen giriş yaptıktan sonra güvenliğiniz için şifrenizi değiştiriniz.</p>
                    <br>
                    <p>İyi çalışmalar,<br>HR Yönetim Ekibi</p>
                </div>";

                    mailMessage.IsBodyHtml = true; // HTML tasarım kullansın

                    smtp.Send(mailMessage);
                }


                ViewBag.Success = "Yeni şifreniz e-posta adresinize başarıyla gönderildi.";
            }
            catch (Exception ex)
            {
                // Olası bir mail hatasında (internet yoksa, şifre yanlışsa vb.)
                ViewBag.Error = "Mail gönderilirken bir hata oluştu: " + ex.Message;
            }

            return View();
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}