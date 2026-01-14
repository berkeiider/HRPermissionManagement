using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRPermissionManagement.Controllers
{
    [Authorize]
    public class LeaveController(AppDbContext context) : Controller
    {
        private readonly AppDbContext _context = context;

        // 1. YÖNETİM LİSTESİ (Admin Tümünü, Müdür Departmanını Görür)
        public IActionResult Index()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            // Kişinin yönettiği departmanları bul
            var managedDepartmentIds = _context.Departments
                                               .Where(d => d.ManagerId == currentUserId)
                                               .Select(d => d.Id)
                                               .ToList();

            bool isManager = managedDepartmentIds.Count != 0;

            IQueryable<LeaveRequest> query = _context.LeaveRequests
                                                     .Include(x => x.Employee)
                                                     .Include(x => x.LeaveType);

            if (isAdmin)
            {
                // ADMIN GÖREVLERİ:
                // 1. Yöneticilerin onayladığı ve son onay bekleyenler (Status == YoneticiOnayladi)
                // 2. Doğrudan Yöneticilerin kendilerinin talep ettiği izinler (Status == Bekliyor ve Talep Eden Bir Yönetici ise)

                // Sistemdeki yöneticilerin ID listesi
                var managerIds = _context.Departments.Select(d => d.ManagerId).Distinct().ToList();

                query = query.Where(x =>
                    x.Status == LeaveStatus.YoneticiOnayladi ||
                    (x.Status == LeaveStatus.Bekliyor && managerIds.Contains(x.EmployeeId))
                );
            }
            else if (isManager)
            {
                // MÜDÜR GÖREVLERİ:
                // Sadece kendi departmanındaki personelin "Bekliyor" taleplerini görür.
                // Kendi talebini burada görmemeli (Onu MyLeaves'de görür).
                query = query.Where(x =>
                    x.EmployeeId != currentUserId && // Kendi talebini onaylayamasın
                    x.Status == LeaveStatus.Bekliyor &&
                    x.Employee != null &&
                    managedDepartmentIds.Contains(x.Employee.DepartmentId)
                );
            }
            else
            {
                // Normal personel bu sayfaya erişirse boş liste dönsün veya kendi sayfasına yönlensin
                return RedirectToAction("MyLeaves");
            }

            var resultList = query.OrderByDescending(x => x.RequestDate).ToList();

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.ManagedDeptIds = managedDepartmentIds;

            return View(resultList);
        }

        // --- KİŞİSEL İZİNLERİM SAYFASI (Herkes Erişebilir) ---
        [HttpGet]
        public IActionResult MyLeaves()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(employeeIdClaim.Value);

            // Sadece giriş yapan kullanıcının izinlerini çekiyoruz
            var myLeaves = _context.LeaveRequests
                                   .Include(x => x.Employee)
                                   .Include(x => x.LeaveType)
                                   .Where(x => x.EmployeeId == currentUserId)
                                   .OrderByDescending(x => x.RequestDate) // En yeni en üstte
                                   .ToList();

            return View(myLeaves);
        }
        // -------------------------------------------------------------------

        // 2. TALEP OLUŞTURMA SAYFASI (GET)
        [HttpGet]
        public IActionResult Create()
        {
            var types = _context.LeaveTypes.ToList();
            // Sıralama: Yıllık İzin (öncelikli), Saatlik İzin (ikinci), diğerleri alfabetik
            var sortedTypes = types.OrderBy(x =>
            {
                if (x.Name == "Yıllık İzin") return 1;
                if (x.Name == "Saatlik İzin") return 2;
                return 3;
            }).ThenBy(x => x.Name).ToList();

            ViewBag.LeaveTypes = sortedTypes;
            return View();
        }

        // 3. TALEP OLUŞTURMA İŞLEMİ (POST)
        [HttpPost]
        public IActionResult Create(LeaveRequest model)
        {
            // 0. İzin Türünü Çek (Saatlik mi değil mi?)
            var leaveType = _context.LeaveTypes.Find(model.LeaveTypeId);
            if (leaveType == null)
            {
                ModelState.AddModelError("", "Geçersiz izin türü.");
                ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                return View(model);
            }

            model.Status = LeaveStatus.Bekliyor;

            // D. Gün Hesabı (Saatlik veya Günlük)
            if (model.StartHour.HasValue && model.EndHour.HasValue)
            {
                // Saatlik İzin
                if (model.StartDate.Date != model.EndDate.Date)
                {
                    ModelState.AddModelError("", "Saatlik izinler aynı gün içinde olmalıdır.");
                    ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                    return View(model);
                }

                if (model.EndHour <= model.StartHour)
                {
                    ModelState.AddModelError("", "Bitiş saati başlangıç saatinden büyük olmalıdır.");
                    var types = _context.LeaveTypes.ToList();
                    ViewBag.LeaveTypes = types.OrderBy(x => { if (x.Name == "Yıllık İzin") return 1; if (x.Name == "Saatlik İzin") return 2; return 3; }).ThenBy(x => x.Name).ToList();
                    return View(model);
                }

                double totalHours = (model.EndHour.Value - model.StartHour.Value).TotalHours;

                if (totalHours < 1)
                {
                    ModelState.AddModelError("", "Saatlik izin en az 1 saat olmalıdır.");
                    ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                    return View(model);
                }

                // Mesai saatini 9 saat varsayıyoruz
                model.NumberOfDays = totalHours / 9.0;
            }
            else
            {
                // Günlük İzin (Eski Mantık)
                TimeSpan fark = model.EndDate - model.StartDate;
                model.NumberOfDays = fark.TotalDays + 1; // Tam gün
            }

            model.RequestDate = DateTime.Now;

            // B. Kullanıcı ID'sini Al
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");
            int empId = int.Parse(employeeIdClaim.Value);
            model.EmployeeId = empId;

            // C. ÇAKIŞAN İZİN KONTROLÜ
            bool isOverlap = _context.LeaveRequests.Any(x =>
                x.EmployeeId == empId &&
                x.Status != LeaveStatus.Reddedildi &&
                (
                    (model.StartDate >= x.StartDate && model.StartDate <= x.EndDate) ||
                    (model.EndDate >= x.StartDate && model.EndDate <= x.EndDate) ||
                    (model.StartDate <= x.StartDate && model.EndDate >= x.EndDate)
                )
            );

            if (isOverlap)
            {
                ModelState.AddModelError("", "Seçtiğiniz tarih aralığında zaten bir izin kaydınız (Bekleyen veya Onaylanan) bulunmaktadır.");
                ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                return View(model);
            }

            _context.LeaveRequests.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "İzin talebiniz başarıyla oluşturuldu.";

            // Talep oluşturduktan sonra "İzinlerim" sayfasına yönlendiriyoruz
            return RedirectToAction("MyLeaves");
        }

        // 4. ONAYLAMA (Logic Update: Personel -> Müdür -> Admin)
        public IActionResult Approve(int id)
        {
            var talep = _context.LeaveRequests
                                .Include(x => x.Employee)
                                .Include(x => x.LeaveType)
                                .FirstOrDefault(x => x.Id == id);

            if (talep == null) return NotFound();

            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            // Yetki Kontrolü: Admin mi veya talep sahibinin yöneticisi mi?
            bool isManagerOfRequester = false;
            if (talep.Employee != null)
            {
                isManagerOfRequester = _context.Departments.Any(d => d.Id == talep.Employee.DepartmentId && d.ManagerId == currentUserId);
            }

            if (!isAdmin && !isManagerOfRequester) return Unauthorized();

            // Yöneticiler kendi izinlerini onaylayamaz (Admin hariç, Admin tek otorite olabilir)
            if (talep.EmployeeId == currentUserId && !isAdmin)
            {
                TempData["Error"] = "Yöneticiler kendi izinlerini onaylayamaz!";
                return RedirectToAction("Index");
            }

            // --- SENARYO 1: YÖNETİCİ ONAYLIYOR (Admin Değil) ---
            if (isManagerOfRequester && !isAdmin && talep.Status == LeaveStatus.Bekliyor)
            {
                // Yönetici onayladığında süreç bitmez, Admin'e düşer.
                talep.Status = LeaveStatus.YoneticiOnayladi;
                _context.SaveChanges();
                TempData["Success"] = "İzin onaylandı ve İnsan Kaynakları'na iletildi.";
            }
            // --- SENARYO 2: ADMIN ONAYLIYOR (Son Karar) ---
            else if (isAdmin)
            {
                // Admin, hem "Bekliyor" (Müdürün izniyse) hem de "YoneticiOnayladi" durumundakileri kesin onaylar.
                if (talep.Status == LeaveStatus.Bekliyor || talep.Status == LeaveStatus.YoneticiOnayladi)
                {
                    talep.Status = LeaveStatus.Onaylandi;

                    // İzin bakiyesinden düş
                    if (talep.LeaveType != null && talep.LeaveType.DoesItAffectBalance)
                    {
                        if (talep.Employee != null)
                        {
                            talep.Employee.AnnualLeaveRight -= talep.NumberOfDays;
                        }
                    }
                    _context.SaveChanges();
                    TempData["Success"] = "İzin talebi kesin olarak onaylandı.";
                }
            }

            return RedirectToAction("Index");
        }

        // 5. REDDETME
        public IActionResult Reject(int id)
        {
            var talep = _context.LeaveRequests.Include(x => x.Employee).FirstOrDefault(x => x.Id == id);
            if (talep == null) return NotFound();

            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            bool isManagerOfRequester = false;
            if (talep.Employee != null)
            {
                isManagerOfRequester = _context.Departments.Any(d => d.Id == talep.Employee.DepartmentId && d.ManagerId == currentUserId);
            }

            if (!isAdmin && !isManagerOfRequester) return Unauthorized();

            // Bekliyor veya YoneticiOnayladi durumundaysa reddedilebilir
            if (talep.Status == LeaveStatus.Bekliyor || talep.Status == LeaveStatus.YoneticiOnayladi)
            {
                talep.Status = LeaveStatus.Reddedildi;
                _context.SaveChanges();
                TempData["Success"] = "Talep reddedildi.";
            }
            return RedirectToAction("Index");
        }

        // 6. PDF DETAY SAYFASI
        public IActionResult Details(int id)
        {
            var talep = _context.LeaveRequests
                                .Include(x => x.Employee)
                                .ThenInclude(e => e!.Department)
                                .Include(x => x.LeaveType)
                                .FirstOrDefault(x => x.Id == id);

            if (talep == null) return NotFound();

            return View(talep);
        }

        // 7. YÖNETİM PANELİ İÇİN İZİN GEÇMİŞİ (Sadece Tamamlananlar) - YENİ EKLENDİ
        public IActionResult History()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            // Yöneticinin departmanlarını bul
            var managedDepartmentIds = _context.Departments
                                               .Where(d => d.ManagerId == currentUserId)
                                               .Select(d => d.Id)
                                               .ToList();
            bool isManager = managedDepartmentIds.Count != 0;

            // Yetkisiz giriş denemesi (Ne admin ne müdürse ana sayfaya at)
            if (!isAdmin && !isManager) return RedirectToAction("MyLeaves");

            // Temel Sorgu: Sadece Onaylanan veya Reddedilen (Biten) İşlemler
            var query = _context.LeaveRequests
                                .Include(x => x.Employee)
                                .ThenInclude(e => e!.Department)
                                .Include(x => x.LeaveType)
                                .Where(x => x.Status == LeaveStatus.Onaylandi || x.Status == LeaveStatus.Reddedildi);

            if (!isAdmin)
            {
                // Yönetici ise SADECE kendi departmanındaki personelleri görsün
                query = query.Where(x => x.Employee != null && managedDepartmentIds.Contains(x.Employee.DepartmentId));
            }

            // Listeyi tarihe göre (en yeni en üstte) sırala
            var historyList = query.OrderByDescending(x => x.StartDate).ToList();

            return View(historyList);
        }
    }
}