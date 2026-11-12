using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.ViewModels;

namespace DemoMVC.Controllers
{
    public class PersonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search = null, int page = 1, int pageSize = 10)
        {
            var people = _context.Persons.AsNoTracking().AsQueryable();
            search = ListSearch.Normalize(search);
            if (search is not null)
            {
                var pattern = ListSearch.ContainsPattern(SqliteSearchFunctions.Normalize(search));
                var hasPersonId = int.TryParse(search, out var personId);
                people = people.Where(x =>
                    EF.Functions.Like(SqliteSearchFunctions.NormalizeForQuery(x.FullName), pattern, "\\")
                    || EF.Functions.Like(SqliteSearchFunctions.NormalizeForQuery(x.Address), pattern, "\\")
                    || (hasPersonId && x.Id == personId));
            }

            var result = await PaginatedListViewModel<Person>.CreateAsync(
                people.OrderBy(x => x.Id),
                search,
                page,
                pageSize);
            if (!IsListPartialRequest
                && (page != result.Pagination.Page || pageSize != result.Pagination.PageSize))
            {
                return RedirectToAction(nameof(Index), new
                {
                    search = result.Pagination.SearchTerm,
                    page = result.Pagination.Page,
                    pageSize = result.Pagination.PageSize
                });
            }

            return IsListPartialRequest ? PartialView("_ListContent", result) : View(result);
        }

        public IActionResult Create()
        {
            return IsAjaxRequest ? PartialView(new Person()) : View(new Person());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Address")] Person person)
        {
            if (!ModelState.IsValid)
            {
                return AjaxView("Create", person);
            }

            _context.Add(person);
            await _context.SaveChangesAsync();
            if (IsAjaxRequest)
            {
                return Json(new { message = "Đã thêm người." });
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var person = await _context.Persons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            return person is null
                ? NotFound()
                : IsAjaxRequest ? PartialView("Details", person) : View("Details", person);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Persons.FindAsync(id);

            if (person == null)
            {
                return NotFound();
            }

            return IsAjaxRequest ? PartialView("Edit", person) : View(person);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Address")] Person person)
        {
            if (id != person.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return AjaxView("Edit", person);
            }

            var existing = await _context.Persons.FindAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            existing.FullName = person.FullName;
            existing.Address = person.Address;
            await _context.SaveChangesAsync();
            if (IsAjaxRequest)
            {
                return Json(new { message = "Đã cập nhật thông tin người." });
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(m => m.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            return IsAjaxRequest ? PartialView("Delete", person) : View(person);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var person = await _context.Persons.FindAsync(id);
            if (person is null)
            {
                return NotFound();
            }

            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();

            if (IsAjaxRequest)
            {
                return Json(new { message = "Đã xóa người." });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool IsAjaxRequest => string.Equals(
            ControllerContext.HttpContext?.Request.Headers["X-Requested-With"].ToString(),
            "XMLHttpRequest",
            StringComparison.Ordinal);
        private bool IsListPartialRequest => string.Equals(
            ControllerContext.HttpContext?.Request.Query["partial"].ToString(),
            "list",
            StringComparison.Ordinal);

        private IActionResult AjaxView(string viewName, Person person)
        {
            if (!IsAjaxRequest)
            {
                return View(viewName, person);
            }

            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView(viewName, person);
        }
    }
}