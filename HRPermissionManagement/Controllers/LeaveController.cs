using HRPermissionManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRPermissionManagement.Controllers
{
    [Authorize]
    public class LeaveController(AppDbContext context, HRPermissionManagement.Helpers.SessionHelper session) : Controller
    {
        private readonly AppDbContext _context = context;
        private readonly HRPermissionManagement.Helpers.SessionHelper _session = session;
        private const double WORK_HOURS = 9.0;

        // 1. YÖNETİM LİSTESİ (Admin Tümünü, Müdür Departmanını Görür)
        public IActionResult Index()
        {
            int? currentUserId = _session.GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            bool isAdmin = _session.IsAdmin();

            // Kişinin yönettiği departmanları bul
            var managedDepartmentIds = _session.GetManagedDepartmentIds(currentUserId.Value);
            bool isManager = managedDepartmentIds.Count != 0;

            IQueryable<LeaveRequest> query = _context.LeaveRequests
                                                     .Include(x => x.Employee)
                                                     .Include(x => x.LeaveType);

            if (isAdmin)
            {
                // ADMIN GÖREVLERİ:
                // 1. Yöneticilerin onayladığı ve son onay bekleyenler (Status == YoneticiOnayladi)
                // 2. Doğrudan Yöneticilerin kendilerinin talep ettiği izinler (Status == Bekliyor ve Talep Eden Bir Yönetici ise)

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
                query = query.Where(x =>
                    x.EmployeeId != currentUserId.Value && // Kendi talebini onaylayamasın
                    x.Status == LeaveStatus.Bekliyor &&
                    x.Employee != null &&
                    managedDepartmentIds.Contains(x.Employee.DepartmentId)
                );
            }
            else
            {
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
            int? currentUserId = _session.GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var myLeaves = _context.LeaveRequests
                                   .Include(x => x.Employee)
                                   .Include(x => x.LeaveType)
                                   .Where(x => x.EmployeeId == currentUserId.Value)
                                   .OrderByDescending(x => x.RequestDate)
                                   .ToList();

            return View(myLeaves);
        }

        // 2. TALEP OLUŞTURMA SAYFASI (GET)
        [HttpGet]
        public IActionResult Create()
        {
            var types = _context.LeaveTypes.ToList();
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
                if (model.StartDate.Date != model.EndDate.Date)
                {
                    ModelState.AddModelError("", "Saatlik izinler aynı gün içinde olmalıdır.");
                    ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                    return View(model);
                }

                if (model.EndHour <= model.StartHour)
                {
                    ModelState.AddModelError("", "Bitiş saati başlangıç saatinden büyük olmalıdır.");
                    ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                    return View(model);
                }

                double totalHours = (model.EndHour.Value - model.StartHour.Value).TotalHours;

                if (totalHours < 1)
                {
                    ModelState.AddModelError("", "Saatlik izin en az 1 saat olmalıdır.");
                    ViewBag.LeaveTypes = _context.LeaveTypes.ToList();
                    return View(model);
                }

                model.NumberOfDays = totalHours / WORK_HOURS;
            }
            else
            {
                TimeSpan fark = model.EndDate - model.StartDate;
                model.NumberOfDays = fark.TotalDays + 1;
            }

            model.RequestDate = DateTime.Now;

            // B. Kullanıcı ID'sini Al
            int? empId = _session.GetCurrentUserId();
            if (empId == null) return RedirectToAction("Login", "Account");
            model.EmployeeId = empId.Value;

            // C. ÇAKIŞAN İZİN KONTROLÜ
            bool isOverlap = _context.LeaveRequests.Any(x =>
                x.EmployeeId == empId.Value &&
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

            int? currentUserId = _session.GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            bool isAdmin = _session.IsAdmin();

            bool isManagerOfRequester = false;
            if (talep.Employee != null)
            {
                isManagerOfRequester = _context.Departments.Any(d => d.Id == talep.Employee.DepartmentId && d.ManagerId == currentUserId.Value);
            }

            if (!isAdmin && !isManagerOfRequester) return Unauthorized();

            if (talep.EmployeeId == currentUserId.Value && !isAdmin)
            {
                TempData["Error"] = "Yöneticiler kendi izinlerini onaylayamaz!";
                return RedirectToAction("Index");
            }

            if (isManagerOfRequester && !isAdmin && talep.Status == LeaveStatus.Bekliyor)
            {
                talep.Status = LeaveStatus.YoneticiOnayladi;
                _context.SaveChanges();
                TempData["Success"] = "İzin onaylandı ve İnsan Kaynakları'na iletildi.";
            }
            else if (isAdmin)
            {
                if (talep.Status == LeaveStatus.Bekliyor || talep.Status == LeaveStatus.YoneticiOnayladi)
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

            int? currentUserId = _session.GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            bool isAdmin = _session.IsAdmin();

            bool isManagerOfRequester = false;
            if (talep.Employee != null)
            {
                isManagerOfRequester = _context.Departments.Any(d => d.Id == talep.Employee.DepartmentId && d.ManagerId == currentUserId.Value);
            }

            if (!isAdmin && !isManagerOfRequester) return Unauthorized();

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

        // 7. YÖNETİM PANELİ İÇİN İZİN GEÇMİŞİ
        public IActionResult History()
        {
            int? currentUserId = _session.GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            bool isAdmin = _session.IsAdmin();

            var managedDepartmentIds = _session.GetManagedDepartmentIds(currentUserId.Value);
            bool isManager = managedDepartmentIds.Count != 0;

            if (!isAdmin && !isManager) return RedirectToAction("MyLeaves");

            var query = _context.LeaveRequests
                                .Include(x => x.Employee)
                                .ThenInclude(e => e!.Department)
                                .Include(x => x.LeaveType)
                                .Where(x => x.Status == LeaveStatus.Onaylandi || x.Status == LeaveStatus.Reddedildi);

            if (!isAdmin)
            {
                query = query.Where(x => x.Employee != null && managedDepartmentIds.Contains(x.Employee.DepartmentId));
            }

            var historyList = query.OrderByDescending(x => x.StartDate).ToList();

            return View(historyList);
        }
    }
}