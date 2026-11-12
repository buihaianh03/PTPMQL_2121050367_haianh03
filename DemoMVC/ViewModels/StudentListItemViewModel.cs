namespace DemoMVC.ViewModels;

public class StudentListItemViewModel
{
    public int Id { get; set; }
    public string MaSV { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public int AdmissionYear { get; set; }
}
