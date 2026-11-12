using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers;

public class TrainingSystemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TrainingSystemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var systems = _context.TrainingSystems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            systems = systems.Where(x => x.Name.Contains(term) || (x.StudentCode != null && x.StudentCode.Contains(term)));
        }

        ViewData["Search"] = search;
        return View(await systems.OrderBy(x => x.Name).ToListAsync());
    }

    public IActionResult Create() => View(new TrainingSystem());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,StudentCode")] TrainingSystem input)
    {
        input.StudentCode = Normalize(input.StudentCode);
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        _context.TrainingSystems.Add(input);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            ModelState.AddModelError(string.Empty, "Tên hoặc mã hệ đã tồn tại.");
            return View(input);
        }

        TempData["Success"] = "Đã thêm hệ đào tạo.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var system = await _context.TrainingSystems.FindAsync(id);
        return system is null ? NotFound() : View(system);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StudentCode")] TrainingSystem input)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        input.StudentCode = Normalize(input.StudentCode);
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var system = await _context.TrainingSystems.FindAsync(id);
        if (system is null)
        {
            return NotFound();
        }

        if (!string.Equals(system.StudentCode, input.StudentCode, StringComparison.Ordinal)
            && await _context.Students.AnyAsync(x => x.TrainingSystemId == id))
        {
            ModelState.AddModelError(nameof(input.StudentCode), "Không thể đổi mã hệ khi đã có sinh viên sử dụng hệ này.");
            return View(input);
        }

        system.Name = input.Name;
        system.StudentCode = input.StudentCode;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            ModelState.AddModelError(string.Empty, "Tên hoặc mã hệ đã tồn tại.");
            return View(input);
        }

        TempData["Success"] = "Đã cập nhật hệ đào tạo.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var system = await _context.TrainingSystems.FindAsync(id);
        return system is null ? NotFound() : View(system);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var system = await _context.TrainingSystems.FindAsync(id);
        if (system is null)
        {
            return NotFound();
        }

        if (await _context.Students.AnyAsync(x => x.TrainingSystemId == id))
        {
            TempData["Error"] = "Không thể xóa hệ đào tạo đang có sinh viên tham chiếu.";
            return RedirectToAction(nameof(Index));
        }

        _context.TrainingSystems.Remove(system);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsForeignKeyConstraint(ex))
        {
            TempData["Error"] = "Không thể xóa hệ đào tạo đang được dữ liệu khác tham chiếu.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa hệ đào tạo.";
        return RedirectToAction(nameof(Index));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUniqueConstraint(DbUpdateException exception) =>
        exception.GetBaseException() is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 };

    private static bool IsForeignKeyConstraint(DbUpdateException exception) =>
        exception.GetBaseException() is SqliteException { SqliteExtendedErrorCode: 787 };
}
