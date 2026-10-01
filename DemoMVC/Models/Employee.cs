using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Employee : Person
    {
        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
        [StringLength(50)]
        [Display(Name = "Mã nhân viên")]
        public string EmployeeId { get; set; } = string.Empty;

        [Range(1, 120, ErrorMessage = "Tuổi phải từ 1 đến 120")]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }
    }
}
