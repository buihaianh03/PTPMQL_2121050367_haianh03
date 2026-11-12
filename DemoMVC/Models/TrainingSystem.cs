using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class TrainingSystem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [RegularExpression(@"^\d{2}$", ErrorMessage = "Mã hệ phải gồm đúng 2 chữ số.")]
    public string? StudentCode { get; set; }

    public ICollection<StudentRecord> Students { get; set; } = new List<StudentRecord>();
}
