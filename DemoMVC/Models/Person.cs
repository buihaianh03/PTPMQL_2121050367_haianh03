using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Person
    {
        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(50, ErrorMessage = "Họ tên tối đa 50 ký tự")]
        public string FullName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(100, ErrorMessage = "Địa chỉ tối đa 100 ký tự")]
        public string Address { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(254)]
        public string? Email { get; set; }
    }
}