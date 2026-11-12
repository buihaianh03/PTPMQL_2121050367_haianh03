namespace DemoMVC.Models;

public class StudentNumberSequence
{
    public int AdmissionYear { get; set; }
    public string TrainingSystemCode { get; set; } = string.Empty;
    public string MajorCode { get; set; } = string.Empty;
    public int LastSequence { get; set; }
}
