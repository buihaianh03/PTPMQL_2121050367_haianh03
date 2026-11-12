using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.ViewModels;

namespace DemoMVC.Controllers;

public class FacultiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FacultiesController> _logger;

    public FacultiesController(ApplicationDbContext context, ILogger<FacultiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var faculties = _context.Faculties.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            faculties = faculties.Where(x => x.Name.Contains(term));
        }

        ViewData["Search"] = search;
        return View(await faculties
            .OrderBy(x => x.Name)
            .Select(x => new FacultyDetailsViewModel
            {
                MaKhoa = x.MaKhoa,
                Name = x.Name
            })
            .ToListAsync());
    }

    public IActionResult Create() => View(new Faculty());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Faculty faculty)
    {
        if (!ModelState.IsValid)
        {
            return View(faculty);
        }

        _context.Faculties.Add(faculty);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            ModelState.AddModelError(nameof(faculty.Name), "Tên khoa đã tồn tại.");
            return View(faculty);
        }

        TempData["Success"] = "Đã thêm khoa.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var faculty = await _context.Faculties.AsNoTracking()
            .Where(x => x.MaKhoa == id)
            .Select(x => new FacultyDetailsViewModel
            {
                MaKhoa = x.MaKhoa,
                Name = x.Name
            })
            .SingleOrDefaultAsync();

        if (faculty is null)
        {
            return NotFound();
        }

        faculty.Students = await _context.Students.AsNoTracking()
            .Where(student => student.MaKhoa == id)
            .OrderBy(student => student.MaSV)
            .Select(student => new StudentListItemViewModel
            {
                Id = student.Id,
                MaSV = student.MaSV,
                FullName = student.FullName,
                MajorName = student.Major.Name,
                FacultyName = faculty.Name,
                AdmissionYear = student.AdmissionYear
            })
            .ToListAsync();

        return View(faculty);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var faculty = await _context.Faculties.FindAsync(id);
        return faculty is null ? NotFound() : View(faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MaKhoa,Name")] Faculty input)
    {
        if (id != input.MaKhoa)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty is null)
        {
            return NotFound();
        }

        faculty.Name = input.Name;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            ModelState.AddModelError(nameof(input.Name), "Tên khoa đã tồn tại.");
            return View(input);
        }

        TempData["Success"] = "Đã cập nhật khoa.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var faculty = await _context.Faculties.FindAsync(id);
        return faculty is null ? NotFound() : View(faculty);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty is null)
        {
            return NotFound();
        }

        if (await _context.Students.AnyAsync(x => x.MaKhoa == id))
        {
            TempData["Error"] = "Không thể xóa khoa đang có sinh viên tham chiếu.";
            return RedirectToAction(nameof(Index));
        }

        _context.Faculties.Remove(faculty);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsForeignKeyConstraint(ex))
        {
            _logger.LogWarning(ex, "Faculty {FacultyId} could not be deleted because it is referenced.", id);
            TempData["Error"] = "Không thể xóa khoa đang có sinh viên tham chiếu.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa khoa.";
        return RedirectToAction(nameof(Index));
    }

    private static bool IsUniqueConstraint(DbUpdateException exception) =>
        exception.GetBaseException() is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 };

    private static bool IsForeignKeyConstraint(DbUpdateException exception) =>
        exception.GetBaseException() is SqliteException { SqliteExtendedErrorCode: 787 };
}
