using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.ViewModels;

namespace DemoMVC.Controllers;

public class StudentRecordsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly StudentNumberService _numberService;
    private readonly StudentRecordService _studentService;

    public StudentRecordsController(
        ApplicationDbContext context,
        StudentNumberService numberService,
        StudentRecordService studentService)
    {
        _context = context;
        _numberService = numberService;
        _studentService = studentService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Students.AsNoTracking()
            .Include(x => x.Major)
            .Include(x => x.Faculty)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.MaSV.Contains(term) || x.FullName.Contains(term));
        }

        ViewData["Search"] = search;
        var students = await query
            .OrderBy(x => x.MaSV)
            .Select(x => new StudentListItemViewModel
            {
                Id = x.Id,
                MaSV = x.MaSV,
                FullName = x.FullName,
                MajorName = x.Major.Name,
                FacultyName = x.Faculty.Name,
                AdmissionYear = x.AdmissionYear
            })
            .ToListAsync();
        return IsRowsRequest ? PartialView("_Rows", students) : View(students);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StudentFormViewModel { AdmissionYear = DateTime.UtcNow.Year };
        await PopulateListsAsync(model);
        return IsAjaxRequest ? PartialView(model) : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewCode(int admissionYear, int trainingSystemId, int majorId)
    {
        try
        {
            var code = await _numberService.PreviewAsync(admissionYear, trainingSystemId, majorId);
            return Json(new { code });
        }
        catch (StudentNumberConfigurationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (StudentNumberExhaustedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateListsAsync(model);
            return AjaxView("Create", model);
        }

        try
        {
            var result = await _studentService.CreateAsync(model);
            switch (result.Status)
            {
                case StudentSaveStatus.Saved:
                    if (IsAjaxRequest)
                    {
                        return Json(new { message = $"Đã thêm sinh viên {result.MaSV}." });
                    }

                    TempData["Success"] = $"Đã thêm sinh viên {result.MaSV}.";
                    return RedirectToAction(nameof(Index));
                case StudentSaveStatus.FacultyNotFound:
                    ModelState.AddModelError(nameof(model.MaKhoa), "Khoa đã chọn không tồn tại.");
                    break;
                case StudentSaveStatus.TrainingSystemNotFound:
                    ModelState.AddModelError(nameof(model.TrainingSystemId), "Hệ đào tạo đã chọn không tồn tại.");
                    break;
                case StudentSaveStatus.MajorNotFound:
                    ModelState.AddModelError(nameof(model.MajorId), "Ngành học đã chọn không tồn tại.");
                    break;
                case StudentSaveStatus.PreviewRequired:
                    ModelState.AddModelError(nameof(model.MaSV), "Hãy tạo mã sinh viên trước khi lưu.");
                    break;
                case StudentSaveStatus.PreviewChanged:
                    model.MaSV = result.MaSV;
                    ModelState.Remove(nameof(model.MaSV));
                    ModelState.AddModelError(nameof(model.MaSV), "Mã vừa thay đổi do có người khác lưu đồng thời. Hãy kiểm tra mã mới và lưu lại.");
                    break;
            }
        }
        catch (StudentNumberConfigurationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (StudentNumberExhaustedException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ModelState.AddModelError(nameof(model.AdmissionYear), ex.Message);
        }

        await PopulateListsAsync(model);
        return AjaxView("Create", model);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var student = await _context.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (student is null)
        {
            return NotFound();
        }

        var model = new StudentFormViewModel
        {
            Id = student.Id,
            MaSV = student.MaSV,
            FullName = student.FullName,
            Address = student.Address,
            AdmissionYear = student.AdmissionYear,
            TrainingSystemId = student.TrainingSystemId,
            MajorId = student.MajorId,
            MaKhoa = student.MaKhoa
        };
        await PopulateListsAsync(model);
        return IsAjaxRequest ? PartialView("Edit", model) : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateListsAsync(model);
            return AjaxView("Edit", model);
        }

        try
        {
            var result = await _studentService.UpdateAsync(id, model);
            switch (result.Status)
            {
                case StudentSaveStatus.Saved:
                    if (IsAjaxRequest)
                    {
                        return Json(new { message = "Đã cập nhật thông tin sinh viên." });
                    }

                    TempData["Success"] = "Đã cập nhật thông tin sinh viên.";
                    return RedirectToAction(nameof(Index));
                case StudentSaveStatus.NotFound:
                    return NotFound();
                case StudentSaveStatus.FacultyNotFound:
                    ModelState.AddModelError(nameof(model.MaKhoa), "Khoa đã chọn không tồn tại.");
                    break;
                case StudentSaveStatus.TrainingSystemNotFound:
                    ModelState.AddModelError(nameof(model.TrainingSystemId), "Hệ đào tạo đã chọn không tồn tại.");
                    break;
                case StudentSaveStatus.MajorNotFound:
                    ModelState.AddModelError(nameof(model.MajorId), "Ngành học đã chọn không tồn tại.");
                    break;
                case StudentSaveStatus.PreviewRequired:
                    ModelState.AddModelError(nameof(model.MaSV), "Khi đổi năm, hệ hoặc ngành, hãy tạo mã mới trước khi lưu.");
                    break;
                case StudentSaveStatus.PreviewChanged:
                    model.MaSV = result.MaSV;
                    ModelState.Remove(nameof(model.MaSV));
                    ModelState.AddModelError(nameof(model.MaSV), "Mã vừa thay đổi do có người khác lưu đồng thời. Hãy kiểm tra mã mới và lưu lại.");
                    break;
            }
        }
        catch (StudentNumberConfigurationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (StudentNumberExhaustedException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ModelState.AddModelError(nameof(model.AdmissionYear), ex.Message);
        }

        await PopulateListsAsync(model);
        return AjaxView("Edit", model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var student = await _context.Students.AsNoTracking()
            .Include(x => x.Faculty)
            .Include(x => x.Major)
            .Include(x => x.TrainingSystem)
            .SingleOrDefaultAsync(x => x.Id == id);
        return student is null
            ? NotFound()
            : IsAjaxRequest ? PartialView("Details", student) : View("Details", student);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var student = await _context.Students.AsNoTracking()
            .Include(x => x.Faculty)
            .SingleOrDefaultAsync(x => x.Id == id);
        return student is null
            ? NotFound()
            : IsAjaxRequest
                ? PartialView("Delete", new StudentListItemViewModel
                {
                    Id = student.Id,
                    MaSV = student.MaSV,
                    FullName = student.FullName,
                    FacultyName = student.Faculty.Name,
                    AdmissionYear = student.AdmissionYear
                })
                : View(new StudentListItemViewModel
            {
                Id = student.Id,
                MaSV = student.MaSV,
                FullName = student.FullName,
                FacultyName = student.Faculty.Name,
                AdmissionYear = student.AdmissionYear
            });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student is null)
        {
            return NotFound();
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();
        if (IsAjaxRequest)
        {
            return Json(new { message = "Đã xóa sinh viên. Mã đã cấp sẽ không được sử dụng lại." });
        }

        TempData["Success"] = "Đã xóa sinh viên. Mã đã cấp sẽ không được sử dụng lại.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest => string.Equals(
        ControllerContext.HttpContext?.Request.Headers["X-Requested-With"].ToString(),
        "XMLHttpRequest",
        StringComparison.Ordinal);
    private bool IsRowsRequest => string.Equals(
        ControllerContext.HttpContext?.Request.Query["partial"].ToString(),
        "rows",
        StringComparison.Ordinal);

    private IActionResult AjaxView(string viewName, StudentFormViewModel model)
    {
        if (!IsAjaxRequest)
        {
            return View(viewName, model);
        }

        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView(viewName, model);
    }

    private async Task PopulateListsAsync(StudentFormViewModel model)
    {
        model.TrainingSystems = await _context.TrainingSystems.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(
                x.StudentCode == null ? $"{x.Name} (chưa cấu hình mã)" : $"{x.Name} ({x.StudentCode})",
                x.Id.ToString(),
                x.Id == model.TrainingSystemId))
            .ToListAsync();
        model.Majors = await _context.Majors.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(
                x.StudentCode == null ? $"{x.Name} (chưa cấu hình mã)" : $"{x.Name} ({x.StudentCode})",
                x.Id.ToString(),
                x.Id == model.MajorId))
            .ToListAsync();
        model.Faculties = await _context.Faculties.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.MaKhoa.ToString(), x.MaKhoa == model.MaKhoa))
            .ToListAsync();
    }
}
