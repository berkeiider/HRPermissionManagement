namespace HRPermissionManagement.Models
{
    public class DashboardViewModel
    {
        public int RemainingLeaveRight { get; set; } // Kalan İzin Hakkı
        public int PendingRequestsCount { get; set; } // Bekleyen Talep Sayısı
        public int TotalEmployeeCount { get; set; }   // Toplam Personel Sayısı
    }
}