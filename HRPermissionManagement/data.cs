using HRPermissionManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPermissionManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        // Başlangıç verilerini ve İlişki Ayarlarını Yapalım
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- İLİŞKİ AYARLARI ---

            // 1. Ana İlişki: Departman -> Çalışanlar (Bir departmanda çok çalışan olur)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict); // Departman silinirse çalışanları silme, hata ver.

            // 2. Yönetici İlişkisi: Departman -> Yönetici (Bir departmanın tek yöneticisi olur)
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany() // Personel tarafında "YönettiğiDepartmanlar" listesi olmadığı için boş bırakıyoruz
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.SetNull); // Yönetici silinirse departmanın yöneticisi boş kalsın.


            // --- SEED DATA (BAŞLANGIÇ VERİLERİ) ---

            // Departmanlar
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Yönetim" },
                new Department { Id = 2, Name = "Yazılım" },
                new Department { Id = 3, Name = "İnsan Kaynakları" }
            );

            // Admin Kullanıcısı (ŞİFRE GÜNCELLENDİ)
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Id = 1,
                    Name = "Admin",
                    Surname = "Yönetici",
                    Email = "admin@sirket.com",
                    // ŞİFRE: "123" yerine MD5 Hashlenmiş hali
                    Password = "VlDqHS/Wf9Zx42uwF2kf4IbXZ8UaBIPU+v79SiOaJsw=:tSMfCNcVqZVrzchaOJ5JwqNpKsV62fGRtBIDQmsB4mE=",
                    DepartmentId = 1,
                    IsAdmin = true,
                    AnnualLeaveRight = 30,
                    StartDate = new DateTime(2024, 1, 1)
                }
            );

            // İzin Türleri
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType { Id = 1, Name = "Yıllık İzin", DoesItAffectBalance = true },
                new LeaveType { Id = 2, Name = "Hastalık Raporu", DoesItAffectBalance = false },
                new LeaveType { Id = 3, Name = "Mazeret İzni", DoesItAffectBalance = true }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}