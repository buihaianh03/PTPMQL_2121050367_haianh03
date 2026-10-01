using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public EmployeeController(ApplicationDbContext context) => _context = context;
        private IQueryable<Employee> Records => _context.Employees;

        public async Task<IActionResult> Index() => View(await Records.AsNoTracking().ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            var model = await Records.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Address,Email,EmployeeId,Age")] Employee model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Employees.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var model = await Records.FirstOrDefaultAsync(p => p.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Address,Email,EmployeeId,Age")] Employee model)
        {
            if (id != model.Id) return NotFound();
            var existing = await Records.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();
            if (!ModelState.IsValid) return View(model);
                existing.FullName = model.FullName;
                existing.Address = model.Address;
                existing.Email = model.Email;
                existing.EmployeeId = model.EmployeeId;
                existing.Age = model.Age;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await Records.AnyAsync(p => p.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var model = await Records.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await Records.FirstOrDefaultAsync(p => p.Id == id);
            if (model == null) return NotFound();
            _context.Employees.Remove(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
