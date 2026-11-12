using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers;

public class MajorsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MajorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var majors = _context.Majors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            majors = majors.Where(x => x.Name.Contains(term)
                || (x.StudentCode != null && x.StudentCode.Contains(term))
                || (x.AdmissionCode != null && x.AdmissionCode.Contains(term)));
        }

        ViewData["Search"] = search;
        var result = await majors.OrderBy(x => x.Name).ToListAsync();
        return IsRowsRequest ? PartialView("_Rows", result) : View(result);
    }

    public IActionResult Create() => IsAjaxRequest ? PartialView(new Major()) : View(new Major());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,AdmissionCode,StudentCode")] Major input)
    {
        Normalize(input);
        if (!ModelState.IsValid)
        {
            return AjaxView("Create", input);
        }

        _context.Majors.Add(input);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            ModelState.AddModelError(string.Empty, "Tên, mã tuyển sinh hoặc mã ngành đã tồn tại.");
            return AjaxView("Create", input);
        }

        if (IsAjaxRequest)
        {
            return Json(new { message = "Đã thêm ngành học." });
        }

        TempData["Success"] = "Đã thêm ngành học.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var major = await _context.Majors.AsNoTracking()
            .Include(x => x.Students)
                .ThenInclude(x => x.Faculty)
            .Include(x => x.Students)
                .ThenInclude(x => x.TrainingSystem)
            .SingleOrDefaultAsync(x => x.Id == id);
        return major is null
            ? NotFound()
            : IsAjaxRequest ? PartialView("Details", major) : View("Details", major);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var major = await _context.Majors.FindAsync(id);
        return major is null ? NotFound() : IsAjaxRequest ? PartialView("Edit", major) : View(major);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AdmissionCode,StudentCode")] Major input)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        Normalize(input);
        if (!ModelState.IsValid)
        {
            return AjaxView("Edit", input);
        }

        var major = await _context.Majors.FindAsync(id);
        if (major is null)
        {
            return NotFound();
        }

        if (!string.Equals(major.StudentCode, input.StudentCode, StringComparison.Ordinal)
            && await _context.Students.AnyAsync(x => x.MajorId == id))
        {
            ModelState.AddModelError(nameof(input.StudentCode), "Không thể đổi mã ngành khi đã có sinh viên sử dụng ngành này.");
            return AjaxView("Edit", input);
        }

        major.Name = input.Name;
        major.AdmissionCode = input.AdmissionCode;
        major.StudentCode = input.StudentCode;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            ModelState.AddModelError(string.Empty, "Tên, mã tuyển sinh hoặc mã ngành đã tồn tại.");
            return AjaxView("Edit", input);
        }

        if (IsAjaxRequest)
        {
            return Json(new { message = "Đã cập nhật ngành học." });
        }

        TempData["Success"] = "Đã cập nhật ngành học.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var major = await _context.Majors.FindAsync(id);
        return major is null ? NotFound() : IsAjaxRequest ? PartialView("Delete", major) : View(major);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var major = await _context.Majors.FindAsync(id);
        if (major is null)
        {
            return NotFound();
        }

        if (await _context.Students.AnyAsync(x => x.MajorId == id))
        {
            if (IsAjaxRequest)
            {
                return Conflict(new { message = "Không thể xóa ngành học đang có sinh viên tham chiếu." });
            }

            TempData["Error"] = "Không thể xóa ngành học đang có sinh viên tham chiếu.";
            return RedirectToAction(nameof(Index));
        }

        _context.Majors.Remove(major);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsForeignKeyConstraint(ex))
        {
            if (IsAjaxRequest)
            {
                return Conflict(new { message = "Không thể xóa ngành học đang được dữ liệu khác tham chiếu." });
            }

            TempData["Error"] = "Không thể xóa ngành học đang được dữ liệu khác tham chiếu.";
            return RedirectToAction(nameof(Index));
        }

        if (IsAjaxRequest)
        {
            return Json(new { message = "Đã xóa ngành học." });
        }

        TempData["Success"] = "Đã xóa ngành học.";
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

    private IActionResult AjaxView(string viewName, Major model)
    {
        if (!IsAjaxRequest)
        {
            return View(viewName, model);
        }

        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView(viewName, model);
    }

    private static void Normalize(Major major)
    {
        major.AdmissionCode = string.IsNullOrWhiteSpace(major.AdmissionCode)
            ? null
            : major.AdmissionCode.Trim();
        major.StudentCode = string.IsNullOrWhiteSpace(major.StudentCode)
            ? null
            : major.StudentCode.Trim();
    }

    private static bool IsUniqueConstraint(DbUpdateException exception) =>
        exception.GetBaseException() is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 };

    private static bool IsForeignKeyConstraint(DbUpdateException exception) =>
        exception.GetBaseException() is SqliteException { SqliteExtendedErrorCode: 787 };
}
