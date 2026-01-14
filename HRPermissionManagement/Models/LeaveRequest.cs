using System.ComponentModel.DataAnnotations;

namespace HRPermissionManagement.Models
{
    public enum LeaveStatus
    {
        Bekliyor = 0,           // Personel talep etti, Müdür onayı bekliyor
        Onaylandi = 1,          // Admin son onayı verdi (Tamamlandı)
        Reddedildi = 2,         // Müdür veya Admin reddetti
        YoneticiOnayladi = 3    // Müdür onayladı, Admin onayı bekliyor (YENİ DURUM)
    }

    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TimeSpan? StartHour { get; set; } // Saatlik izin başlangıç saati
        public TimeSpan? EndHour { get; set; }   // Saatlik izin bitiş saati

        public double NumberOfDays { get; set; } // int -> double (0.5 gün vb. için)

        // Açıklama zorunlu olmasın, boş olabilir
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