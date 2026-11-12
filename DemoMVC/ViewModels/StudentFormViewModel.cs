using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DemoMVC.ViewModels;

public class StudentFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Mã sinh viên")]
    public string? MaSV { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(200)]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn năm nhập học.")]
    [Range(2000, 2099, ErrorMessage = "Năm nhập học phải từ 2000 đến 2099.")]
    [Display(Name = "Năm nhập học")]
    public int AdmissionYear { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hệ đào tạo.")]
    [Display(Name = "Hệ đào tạo")]
    public int? TrainingSystemId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngành học.")]
    [Display(Name = "Ngành học")]
    public int? MajorId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khoa.")]
    [Display(Name = "Khoa")]
    public int? MaKhoa { get; set; }

    public IEnumerable<SelectListItem> TrainingSystems { get; set; } = [];
    public IEnumerable<SelectListItem> Majors { get; set; } = [];
    public IEnumerable<SelectListItem> Faculties { get; set; } = [];
}
