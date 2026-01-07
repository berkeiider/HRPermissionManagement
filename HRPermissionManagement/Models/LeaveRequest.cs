using System.ComponentModel.DataAnnotations;

namespace HRPermissionManagement.Models
{
    public enum LeaveStatus
    {
        Bekliyor = 0,
        Onaylandi = 1,
        Reddedildi = 2
    }

    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NumberOfDays { get; set; }

        // Açıklama zorunlu olmasın, boş olabilir (?)
        public string? Description { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;
        public LeaveStatus Status { get; set; } = LeaveStatus.Bekliyor;

        // İlişkiler
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; } // Veri tabanından çekilmezse null olabilir

        public int LeaveTypeId { get; set; }
        public LeaveType? LeaveType { get; set; } // Veri tabanından çekilmezse null olabilir
    }
}