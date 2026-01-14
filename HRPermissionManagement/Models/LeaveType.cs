using System.ComponentModel.DataAnnotations;

namespace HRPermissionManagement.Models
{
    public class LeaveType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Varsayılan değer
        public bool DoesItAffectBalance { get; set; }
        public bool IsHourly { get; set; } // Saatlik izin mi?
    }
}