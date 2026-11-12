using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class Major
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [StringLength(12)]
    public string? AdmissionCode { get; set; }

    [RegularExpression(@"^\d{2}$", ErrorMessage = "Mã ngành dùng trong mã sinh viên phải gồm đúng 2 chữ số.")]
    public string? StudentCode { get; set; }

    public ICollection<StudentRecord> Students { get; set; } = new List<StudentRecord>();
}
