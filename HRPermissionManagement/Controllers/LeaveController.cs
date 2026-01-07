using HRPermissionManagement.Data;
using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRPermissionManagement.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly AppDbContext _context;

        public LeaveController(AppDbContext context)
        {
            _context = context;
        }

        // 1. YÖNETİM LİSTESİ (Admin Tümünü, Müdür Departmanını Görür)
        public IActionResult Index()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            var managedDepartmentIds = _context.Departments
                                               .Where(d => d.ManagerId == currentUserId)
                                               .Select(d => d.Id)
                                               .ToList();

            bool isManager = managedDepartmentIds.Any();

            List<LeaveRequest> talepler;

            if (isAdmin)
            {
                talepler = _context.LeaveRequests
                                   .Include(x => x.Employee)
                                   .Include(x => x.LeaveType)
                                   .OrderByDescending(x => x.RequestDate)
                                   .ToList();
            }
            else if (isManager)
            {
                talepler = _context.LeaveRequests
                                   .Include(x => x.Employee)
                                   .Include(x => x.LeaveType)
                                   .Where(x => x.EmployeeId == currentUserId ||
                                               (x.Employee != null && managedDepartmentIds.Contains(x.Employee.DepartmentId)))
                                   .OrderByDescending(x => x.RequestDate)
                                   .ToList();
            }
            else
            {
                // Normal personel Index'e girerse sadece kendisininkini görür
                // (Ama biz artık onlar için MyLeaves kullanacağız)
                talepler = _context.LeaveRequests
                                   .Include(x => x.Employee)
                                   .Include(x => x.LeaveType)
                                   .Where(x => x.EmployeeId == currentUserId)
                                   .OrderByDescending(x => x.RequestDate)
                                   .ToList();
            }

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.ManagedDeptIds = managedDepartmentIds;

            return View(talepler);
        }

        // --- YENİ EKLENEN: KİŞİSEL İZİNLERİM SAYFASI (Herkes Erişebilir) ---
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
            ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
            return View();
        }

        // 3. TALEP OLUŞTURMA İŞLEMİ (POST)
        [HttpPost]
        public IActionResult Create(LeaveRequest model)
        {
            // A. Tarih Mantık Kontrolü
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "Bitiş tarihi başlangıç tarihinden küçük olamaz!");
                ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                return View(model);
            }

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

            // D. Gün Hesapla ve Kaydet
            TimeSpan fark = model.EndDate - model.StartDate;
            model.NumberOfDays = (int)fark.TotalDays + 1;

            model.RequestDate = DateTime.Now;
            model.Status = LeaveStatus.Bekliyor;

            _context.LeaveRequests.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "İzin talebiniz başarıyla oluşturuldu.";

            // Talep oluşturduktan sonra "İzinlerim" sayfasına yönlendiriyoruz
            return RedirectToAction("MyLeaves");
        }

        // 4. ONAYLAMA
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

            bool isManagerOfRequester = false;
            if (talep.Employee != null)
            {
                isManagerOfRequester = _context.Departments.Any(d => d.Id == talep.Employee.DepartmentId && d.ManagerId == currentUserId);
            }

            if (!isAdmin && !isManagerOfRequester) return Unauthorized();

            if (talep.EmployeeId == currentUserId && !isAdmin)
            {
                TempData["Error"] = "Yöneticiler kendi izinlerini onaylayamaz!";
                return RedirectToAction("Index");
            }

            if (talep.Status == LeaveStatus.Bekliyor)
            {
                talep.Status = LeaveStatus.Onaylandi;

                if (talep.LeaveType != null && talep.LeaveType.DoesItAffectBalance)
                {
                    if (talep.Employee != null)
                    {
                        talep.Employee.AnnualLeaveRight -= talep.NumberOfDays;
                    }
                }
                _context.SaveChanges();
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

            if (talep.Status == LeaveStatus.Bekliyor)
            {
                talep.Status = LeaveStatus.Reddedildi;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 6. PDF DETAY SAYFASI
        public IActionResult Details(int id)
        {
            var talep = _context.LeaveRequests
                                .Include(x => x.Employee)
                                .ThenInclude(e => e.Department)
                                .Include(x => x.LeaveType)
                                .FirstOrDefault(x => x.Id == id);

            if (talep == null) return NotFound();

            return View(talep);
        }
    }
}