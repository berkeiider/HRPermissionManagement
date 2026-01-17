using Microsoft.EntityFrameworkCore;
using HRPermissionManagement.Models;
using System.Security.Claims;

namespace HRPermissionManagement.Helpers
{
    public class SessionHelper(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public int? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var claim = user.FindFirst("EmployeeId");
            if (claim == null) return null;

            if (int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Returns the list of Department IDs that the current user manages.
        /// </summary>
        public List<int> GetManagedDepartmentIds(int userId)
        {
            return _context.Departments
                           .Where(d => d.ManagerId == userId)
                           .Select(d => d.Id)
                           .ToList();
        }

        public bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
        }
    }
}
