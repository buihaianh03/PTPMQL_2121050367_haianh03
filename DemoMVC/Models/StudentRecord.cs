using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class StudentRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Mã sinh viên phải gồm đúng 10 chữ số.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã sinh viên phải gồm đúng 10 chữ số.")]
    public string MaSV { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự.")]
    public string Address { get; set; } = string.Empty;

    [Range(2000, 2099, ErrorMessage = "Năm nhập học phải từ 2000 đến 2099.")]
    public int AdmissionYear { get; set; }

    public int TrainingSystemId { get; set; }
    public TrainingSystem TrainingSystem { get; set; } = null!;

    public int MajorId { get; set; }
    public Major Major { get; set; } = null!;

    public int MaKhoa { get; set; }
    public Faculty Faculty { get; set; } = null!;
}
