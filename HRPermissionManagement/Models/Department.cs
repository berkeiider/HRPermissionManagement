using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Bunu eklemeyi unutma

namespace HRPermissionManagement.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Departman adı zorunludur.")]
        [Display(Name = "Departman Adı")]
        public string Name { get; set; } = string.Empty;

        // YENİ ALAN: Departman Sorumlusu (Boş olabilir, çünkü başta kimse atanmamış olabilir)
        public int? ManagerId { get; set; }

        // İlişki: Yönetici bir Personeldir
        [ForeignKey("ManagerId")]
        public Employee? Manager { get; set; }

        public List<Employee> Employees { get; set; } = new List<Employee>();
    }
}