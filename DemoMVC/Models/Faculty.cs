using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class Faculty
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaKhoa { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khoa.")]
    [StringLength(120, ErrorMessage = "Tên khoa không được vượt quá 120 ký tự.")]
    public string Name { get; set; } = string.Empty;

    public ICollection<StudentRecord> Students { get; set; } = new List<StudentRecord>();
}
