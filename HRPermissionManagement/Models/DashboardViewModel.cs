using System.Collections.Generic;

namespace HRPermissionManagement.Models
{
    public class DashboardViewModel
    {
        public double RemainingLeaveRight { get; set; } // Kalan İzin Hakkı
        public int PendingRequestsCount { get; set; } // Bekleyen Talep Sayısı
        public int TotalEmployeeCount { get; set; }   // Toplam Personel Sayısı

        // YENİ EKLENEN: Yaklaşan izinleri tutacak liste
        public List<LeaveRequest> UpcomingLeaves { get; set; } = [];
    }
}