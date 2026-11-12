namespace DemoMVC.ViewModels;

public class FacultyDetailsViewModel
{
    public int MaKhoa { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<StudentListItemViewModel> Students { get; set; } = [];
}
