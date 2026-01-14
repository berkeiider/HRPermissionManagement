using HRPermissionManagement;
using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRPermissionManagement.Controllers
{
    [Authorize]
    public class HomeController(AppDbContext context) : Controller
    {
        private readonly AppDbContext _context = context;

        public IActionResult Index()
        {
            // 1. Güvenlik: Kullanıcı ID'sini al
            var employeeIdClaim = User.FindFirst("EmployeeId");
            if (employeeIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(employeeIdClaim.Value);
            bool isAdmin = User.IsInRole("Admin");

            // --- YÖNETİCİ KONTROLÜ ---
            // Giriş yapan kişi hangi departmanları yönetiyor?
            var managedDepartmentIds = _context.Departments
                                               .Where(d => d.ManagerId == userId)
                                               .Select(d => d.Id)
                                               .ToList();
            bool isManager = managedDepartmentIds.Count != 0;
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
                // Admin: HER ŞEYİ sayar (Direkt Bekleyenler + Yönetici Onaylamış İK Bekleyenler)
                model.PendingRequestsCount = _context.LeaveRequests.Count(x =>
                    x.Status == LeaveStatus.Bekliyor ||
                    x.Status == LeaveStatus.YoneticiOnayladi);
                model.TotalEmployeeCount = _context.Employees.Count();
            }
            else if (isManager)
            {
                // Müdür: Sadece kendi ekibinin veya kendisinin taleplerini sayar
                model.PendingRequestsCount = _context.LeaveRequests
                    .Count(x => x.Status == LeaveStatus.Bekliyor &&
                                (x.EmployeeId == userId ||
                                 (x.Employee != null && managedDepartmentIds.Contains(x.Employee.DepartmentId))));

                // Yöneticiye özel toplam personel sayısı (Sadece kendi ekibini görsün)
                model.TotalEmployeeCount = _context.Employees.Count(x => managedDepartmentIds.Contains(x.DepartmentId));
            }
            else
            {
                // Standart Personel: SADECE KENDİ taleplerini sayar
                model.PendingRequestsCount = _context.LeaveRequests.Count(x => x.Status == LeaveStatus.Bekliyor && x.EmployeeId == userId);
                model.TotalEmployeeCount = 0;
            }

            // --- YENİ EKLENEN KISIM: YAKLAŞAN İZİNLER TABLOSU ---
            // Sadece Admin veya Yöneticiler bu veriyi doldurur
            if (isAdmin || isManager)
            {
                DateTime today = DateTime.Today;
                DateTime nextMonth = today.AddDays(30); // Önümüzdeki 30 güne bakıyoruz

                var query = _context.LeaveRequests
                                    .Include(x => x.Employee)
                                    .ThenInclude(e => e!.Department)
                                    .Include(x => x.LeaveType)
                                    .Where(x => x.Status == LeaveStatus.Onaylandi && // Sadece onaylanmışlar
                                                x.StartDate >= today &&
                                                x.StartDate <= nextMonth);

                if (!isAdmin && isManager)
                {
                    // Müdür ise sadece kendi departmanındakileri görsün
                    query = query.Where(x => x.Employee != null && managedDepartmentIds.Contains(x.Employee.DepartmentId));
                }

                // Tarihe göre sırala ve ilk 10 tanesini al
                model.UpcomingLeaves = [.. query.OrderBy(x => x.StartDate).Take(10)];
            }
            // ----------------------------------------------------

            // --- GRAFİK VERİLERİ ---
            // Departman adlarını ve çalışan sayılarını gruplayarak alıyoruz
            var departmanDagilimi = _context.Employees
                                            .Include(e => e.Department)
                                            .Where(e => e.Department != null)
                                            .GroupBy(e => e.Department!.Name)
                                            .Select(g => new { Name = g.Key, Count = g.Count() })
                                            .ToList();

            // View tarafında JavaScript okuyabilsin diye bu verileri ayrı ayrı listelere alıyoruz
            ViewBag.DeptNames = departmanDagilimi.Select(x => x.Name).ToList();
            ViewBag.DeptCounts = departmanDagilimi.Select(x => x.Count).ToList();

            return View(model);
        }
    }
}