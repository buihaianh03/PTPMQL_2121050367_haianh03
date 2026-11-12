using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class Faculty
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaKhoa { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public ICollection<StudentRecord> Students { get; set; } = new List<StudentRecord>();
}
