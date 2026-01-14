using System.ComponentModel.DataAnnotations;

namespace HRPermissionManagement.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Surname { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)] // 50 yerine 255 yapın
        public string Password { get; set; } = string.Empty;

        public double AnnualLeaveRight { get; set; } = 14;
        public bool IsAdmin { get; set; } = false;
        public DateTime StartDate { get; set; }

        // İlişkiler
        public int DepartmentId { get; set; }

        // DİKKAT: Department verisi her zaman yüklü olmayabilir, bu yüzden '?' koyduk
        public Department? Department { get; set; }

        public List<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}