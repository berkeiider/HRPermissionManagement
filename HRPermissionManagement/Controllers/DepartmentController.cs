using HRPermissionManagement.Data;
using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPermissionManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME (Index)
        public IActionResult Index()
        {
            var departmanlar = _context.Departments.ToList();
            return View(departmanlar);
        }

        // 2. EKLEME SAYFASI (GET) - Formu Gösterir
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. EKLEME İŞLEMİ (POST) - Veriyi Kaydeder
        [HttpPost]
        public IActionResult Create(Department model)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // 4. DÜZENLEME SAYFASI (GET)
        // Adım 3 Entegrasyonu: Dropdown'ı burada dolduruyoruz
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var departman = _context.Departments.Find(id);
            if (departman == null)
            {
                return NotFound();
            }

            // --- YÖNETİCİ SEÇİMİ İÇİN EKLENEN KISIM ---
            // O departmana ait personelleri çekip ViewBag'e atıyoruz.
            // Sadece o departmanın çalışanları yönetici olabilir.
            ViewBag.Employees = _context.Employees.Where(x => x.DepartmentId == id).ToList();
            // -------------------------------------------

            return View(departman);
        }

        // 5. DÜZENLEME İŞLEMİ (POST)
        [HttpPost]
        public IActionResult Edit(Department model)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Hata olursa (örneğin boş isim girilirse) dropdown boş gelmesin diye tekrar dolduruyoruz
            ViewBag.Employees = _context.Employees.Where(x => x.DepartmentId == model.Id).ToList();

            return View(model);
        }

        // 6. SİLME İŞLEMİ (GÜNCELLENDİ)
        public IActionResult Delete(int id)
        {
            // 1. Önce bu departmanda çalışan personel var mı kontrol et
            bool hasEmployees = _context.Employees.Any(x => x.DepartmentId == id);

            if (hasEmployees)
            {
                // Varsa silme, hata mesajı gönder
                TempData["Error"] = "Bu departmanda çalışan personeller olduğu için silinemez! Önce personelleri başka departmana taşıyın veya silin.";
                return RedirectToAction("Index");
            }

            // 2. Kimse yoksa silme işlemine devam et
            var departman = _context.Departments.Find(id);
            if (departman != null)
            {
                _context.Departments.Remove(departman);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}