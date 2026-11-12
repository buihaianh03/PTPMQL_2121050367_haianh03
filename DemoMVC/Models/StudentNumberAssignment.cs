using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models;

public class StudentNumberAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 10)]
    public string MaSV { get; set; } = string.Empty;

    public int AdmissionYear { get; set; }
    public string TrainingSystemCode { get; set; } = string.Empty;
    public string MajorCode { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }

    public int? StudentId { get; set; }
    public StudentRecord? Student { get; set; }

    public DateTime AssignedAtUtc { get; set; }
}
