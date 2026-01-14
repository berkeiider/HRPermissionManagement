using HRPermissionManagement.Data;
using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRPermissionManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public IActionResult Index()
        {
            // Departman tablosunu da dahil ederek (Include) çekiyoruz
            var personeller = _context.Employees.Include(x => x.Department).ToList();
            return View(personeller);
        }

        // 2. EKLEME SAYFASI (GET)
        [HttpGet]
        public IActionResult Create()
        {
            // Dropdown'ı doldurmak için departmanları View'a gönderiyoruz
            ViewBag.Departmanlar = _context.Departments.ToList();
            return View();
        }

        // 3. EKLEME İŞLEMİ (POST)
        [HttpPost]
        public IActionResult Create(Employee model)
        {
            if (ModelState.IsValid)
            {
                // Yeni kayıtta şifre boş olamaz, hashleyip kaydediyoruz
                model.Password = HRPermissionManagement.Helpers.Hasher.HashPassword(model.Password);

                _context.Employees.Add(model);
                _context.SaveChanges();

                TempData["Success"] = "Personel başarıyla eklendi.";
                return RedirectToAction("Index");
            }

            // Hata varsa sayfayı tekrar yükle (Dropdown'ı tekrar doldurarak)
            ViewBag.Departmanlar = _context.Departments.ToList();
            return View(model);
        }

        // 4. DÜZENLEME SAYFASI (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var personel = _context.Employees.Find(id);
            if (personel == null)
            {
                return NotFound();
            }

            // --- YENİ EKLENEN: Hashli şifreyi arayüzde gösterme ---
            personel.Password = "";
            // -----------------------------------------------------

            // Dropdown için departmanları tekrar yüklüyoruz
            ViewBag.Departmanlar = _context.Departments.ToList();

            return View(personel);
        }

        // 5. DÜZENLEME İŞLEMİ (POST)
        [HttpPost]
        public IActionResult Edit(Employee model)
        {
            // Veritabanındaki mevcut (eski) kaydı, takip edilmeyen (NoTracking) modda çekiyoruz.
            var mevcutPersonel = _context.Employees.AsNoTracking().FirstOrDefault(x => x.Id == model.Id);

            if (ModelState.IsValid)
            {
                // --- ŞİFRE KONTROL MANTIĞI ---
                if (string.IsNullOrEmpty(model.Password))
                {
                    // EĞER KUTU BOŞSA: Kullanıcı şifreyi değiştirmek istemiyor.
                    // Veritabanındaki eski şifreyi modele geri yükle.
                    model.Password = mevcutPersonel.Password;
                }
                else
                {
                    // EĞER KUTU DOLUYSA: Kullanıcı yeni şifre girmiş.
                    // Yeni şifreyi Hash'le
                    model.Password = HRPermissionManagement.Helpers.Hasher.HashPassword(model.Password);
                }
                // -----------------------------

                _context.Employees.Update(model);
                _context.SaveChanges();

                TempData["Success"] = "Personel bilgileri güncellendi.";
                return RedirectToAction("Index");
            }

            // Hata varsa dropdown tekrar dolsun
            ViewBag.Departmanlar = _context.Departments.ToList();
            return View(model);
        }

        // 6. SİLME İŞLEMİ
        public IActionResult Delete(int id)
        {
            var personel = _context.Employees.Find(id);
            if (personel != null)
            {
                _context.Employees.Remove(personel);
                _context.SaveChanges();
                TempData["Success"] = "Personel silindi.";
            }
            return RedirectToAction("Index");
        }
    }
}