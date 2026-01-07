using HRPermissionManagement.Data;
using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Include işlemi için gerekli olabilir

namespace HRPermissionManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Güvenlik: Kullanıcı ID'sini al
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            // --- YÖNETİCİ KONTROLÜ (Burası Eklendi) ---
            // Giriş yapan kişi hangi departmanları yönetiyor?
            var managedDepartmentIds = _context.Departments
                                               .Where(d => d.ManagerId == userId)
                                               .Select(d => d.Id)
                                               .ToList();
            bool isManager = managedDepartmentIds.Any();
            // ------------------------------------------

            // 2. ViewModel'i hazırla
            var model = new DashboardViewModel();

            // A. Kalan İzin Hakkı (Herkes için aynı)
            var personel = _context.Employees.Find(userId);
            if (personel != null)
            {
                model.RemainingLeaveRight = personel.AnnualLeaveRight;
            }

            // B. Bekleyen Talepler ve Toplam Personel
            if (isAdmin)
            {
                // Admin: HER ŞEYİ sayar
                model.PendingRequestsCount = _context.LeaveRequests.Count(x => x.Status == LeaveStatus.Bekliyor);
                model.TotalEmployeeCount = _context.Employees.Count();
            }
            else if (isManager)
            {
              
                // (x.Employee != null kontrolü ile birlikte)
                model.PendingRequestsCount = _context.LeaveRequests
                    .Count(x => x.Status == LeaveStatus.Bekliyor &&
                                (x.EmployeeId == userId ||
                                 (x.Employee != null && managedDepartmentIds.Contains(x.Employee.DepartmentId))));

                // Yöneticiye özel toplam personel sayısı (Opsiyonel: Sadece kendi ekibini görsün)
                model.TotalEmployeeCount = _context.Employees.Count(x => managedDepartmentIds.Contains(x.DepartmentId));
            }
            else
            {
                // Standart Personel: SADECE KENDİ taleplerini sayar
                model.PendingRequestsCount = _context.LeaveRequests.Count(x => x.Status == LeaveStatus.Bekliyor && x.EmployeeId == userId);
                model.TotalEmployeeCount = 0;
            }
            // --- GRAFİK VERİLERİ ---
            // Departman adlarını ve çalışan sayılarını gruplayarak alıyoruz
            var departmanDagilimi = _context.Employees
                                            .Include(e => e.Department)
                                            .GroupBy(e => e.Department.Name)
                                            .Select(g => new { Name = g.Key, Count = g.Count() })
                                            .ToList();

            // View tarafında JavaScript okuyabilsin diye bu verileri ayrı ayrı listelere alıyoruz
            ViewBag.DeptNames = departmanDagilimi.Select(x => x.Name).ToList();
            ViewBag.DeptCounts = departmanDagilimi.Select(x => x.Count).ToList();
            return View(model);
        }
    }
}