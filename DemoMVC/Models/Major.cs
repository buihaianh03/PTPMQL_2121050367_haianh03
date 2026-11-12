using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class Major
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên ngành học.")]
    [StringLength(160, ErrorMessage = "Tên ngành học không được vượt quá 160 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(12, ErrorMessage = "Mã tuyển sinh không được vượt quá 12 ký tự.")]
    public string? AdmissionCode { get; set; }

    [RegularExpression(@"^\d{2}$", ErrorMessage = "Mã ngành dùng trong mã sinh viên phải gồm đúng 2 chữ số.")]
    public string? StudentCode { get; set; }

    public ICollection<StudentRecord> Students { get; set; } = new List<StudentRecord>();
}
