using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class StudentRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Mã sinh viên phải gồm đúng 10 chữ số.")]
    public string MaSV { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Range(2000, 2099)]
    public int AdmissionYear { get; set; }

    public int TrainingSystemId { get; set; }
    public TrainingSystem TrainingSystem { get; set; } = null!;

    public int MajorId { get; set; }
    public Major Major { get; set; } = null!;

    public int MaKhoa { get; set; }
    public Faculty Faculty { get; set; } = null!;
}
