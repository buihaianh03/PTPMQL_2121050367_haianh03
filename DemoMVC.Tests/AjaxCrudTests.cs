using DemoMVC.Controllers;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DemoMVC.Tests;

public sealed class AjaxCrudTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ApplicationDbContext _context = null!;
    private Faculty _faculty = null!;
    private TrainingSystem _trainingSystem = null!;
    private Major _major = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options);
        await _context.Database.EnsureCreatedAsync();

        _faculty = new Faculty { Name = "Khoa kiểm thử" };
        _trainingSystem = new TrainingSystem { Name = "Hệ kiểm thử", StudentCode = "81" };
        _major = new Major { Name = "Ngành kiểm thử", AdmissionCode = "7480201", StudentCode = "82" };
        _context.AddRange(_faculty, _trainingSystem, _major);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CatalogCrudActionsUseAjaxResultsAndPersistChanges()
    {
        var faculties = WithAjax(new FacultiesController(
            _context,
            NullLogger<FacultiesController>.Instance), rows: true);
        Assert.IsType<PartialViewResult>(faculties.Create());
        var faculty = new Faculty { Name = "Khoa AJAX" };
        Assert.IsType<JsonResult>(await faculties.Create(faculty));
        Assert.True(await _context.Faculties.AnyAsync(x => x.Name == faculty.Name));
        Assert.IsType<PartialViewResult>(await faculties.Details(faculty.MaKhoa));
        Assert.IsType<PartialViewResult>(await faculties.Edit(faculty.MaKhoa));
        faculty.Name = "Khoa AJAX đã sửa";
        Assert.IsType<JsonResult>(await faculties.Edit(faculty.MaKhoa, faculty));
        Assert.IsType<PartialViewResult>(await faculties.Delete(faculty.MaKhoa));
        Assert.IsType<JsonResult>(await faculties.DeleteConfirmed(faculty.MaKhoa));
        Assert.False(await _context.Faculties.AnyAsync(x => x.MaKhoa == faculty.MaKhoa));
        Assert.IsType<PartialViewResult>(await faculties.Index(null));

        var majors = WithAjax(new MajorsController(_context), rows: true);
        Assert.IsType<PartialViewResult>(majors.Create());
        var major = new Major { Name = "Ngành AJAX", StudentCode = "83", AdmissionCode = "7480202" };
        Assert.IsType<JsonResult>(await majors.Create(major));
        Assert.True(await _context.Majors.AnyAsync(x => x.Id == major.Id));
        Assert.IsType<PartialViewResult>(await majors.Details(major.Id));
        Assert.IsType<PartialViewResult>(await majors.Edit(major.Id));
        major.Name = "Ngành AJAX đã sửa";
        Assert.IsType<JsonResult>(await majors.Edit(major.Id, major));
        Assert.IsType<PartialViewResult>(await majors.Delete(major.Id));
        Assert.IsType<JsonResult>(await majors.DeleteConfirmed(major.Id));
        Assert.False(await _context.Majors.AnyAsync(x => x.Id == major.Id));
        Assert.IsType<PartialViewResult>(await majors.Index(null));

        var systems = WithAjax(new TrainingSystemsController(_context), rows: true);
        Assert.IsType<PartialViewResult>(systems.Create());
        var system = new TrainingSystem { Name = "Hệ AJAX", StudentCode = "84" };
        Assert.IsType<JsonResult>(await systems.Create(system));
        Assert.True(await _context.TrainingSystems.AnyAsync(x => x.Id == system.Id));
        Assert.IsType<PartialViewResult>(await systems.Details(system.Id));
        Assert.IsType<PartialViewResult>(await systems.Edit(system.Id));
        system.Name = "Hệ AJAX đã sửa";
        Assert.IsType<JsonResult>(await systems.Edit(system.Id, system));
        Assert.IsType<PartialViewResult>(await systems.Delete(system.Id));
        Assert.IsType<JsonResult>(await systems.DeleteConfirmed(system.Id));
        Assert.False(await _context.TrainingSystems.AnyAsync(x => x.Id == system.Id));
        Assert.IsType<PartialViewResult>(await systems.Index(null));

        var people = WithAjax(new PersonController(_context), rows: true);
        Assert.IsType<PartialViewResult>(people.Create());
        var person = new Person { FullName = "Người kiểm thử", Address = "Hà Nội" };
        Assert.IsType<JsonResult>(await people.Create(person));
        Assert.True(await _context.Persons.AnyAsync(x => x.Id == person.Id));
        Assert.IsType<PartialViewResult>(await people.Details(person.Id));
        Assert.IsType<PartialViewResult>(await people.Edit(person.Id));
        person.FullName = "Người AJAX đã sửa";
        Assert.IsType<JsonResult>(await people.Edit(person.Id, person));
        Assert.IsType<PartialViewResult>(await people.Delete(person.Id));
        Assert.IsType<JsonResult>(await people.DeleteConfirmed(person.Id));
        Assert.False(await _context.Persons.AnyAsync(x => x.Id == person.Id));
        Assert.IsType<PartialViewResult>(await people.Index());
    }

    [Fact]
    public async Task StudentCrudActionsUseAjaxAndPreserveNumberHistory()
    {
        var numbers = new StudentNumberService(_context);
        var controller = WithAjax(new StudentRecordsController(
            _context,
            numbers,
            new StudentRecordService(_context, numbers)), rows: true);

        Assert.IsType<PartialViewResult>(await controller.Create());
        var input = new StudentFormViewModel
        {
            MaSV = await numbers.PreviewAsync(2028, _trainingSystem.Id, _major.Id),
            FullName = "Sinh viên kiểm thử",
            Address = "Hà Nội",
            AdmissionYear = 2028,
            TrainingSystemId = _trainingSystem.Id,
            MajorId = _major.Id,
            MaKhoa = _faculty.MaKhoa
        };
        Assert.IsType<JsonResult>(await controller.Create(input));
        var student = await _context.Students.SingleAsync();
        Assert.IsType<PartialViewResult>(await controller.Details(student.Id));
        Assert.IsType<PartialViewResult>(await controller.Edit(student.Id));

        input.Id = student.Id;
        input.MaSV = student.MaSV;
        input.FullName = "Sinh viên đã sửa";
        Assert.IsType<JsonResult>(await controller.Edit(student.Id, input));
        Assert.Equal("Sinh viên đã sửa", (await _context.Students.SingleAsync()).FullName);
        Assert.IsType<PartialViewResult>(await controller.Delete(student.Id));
        Assert.IsType<JsonResult>(await controller.DeleteConfirmed(student.Id));
        Assert.Empty(await _context.Students.ToListAsync());
        Assert.Null((await _context.StudentNumberAssignments.SingleAsync()).StudentId);

        Assert.IsType<PartialViewResult>(await controller.Index(null));
    }

    [Fact]
    public async Task ReferencedCatalogRowsReturnAjaxConflictInsteadOfDeleting()
    {
        var numbers = new StudentNumberService(_context);
        var students = new StudentRecordsController(
            _context,
            numbers,
            new StudentRecordService(_context, numbers));
        var input = new StudentFormViewModel
        {
            MaSV = await numbers.PreviewAsync(2028, _trainingSystem.Id, _major.Id),
            FullName = "Sinh viên giữ tham chiếu",
            Address = "Hà Nội",
            AdmissionYear = 2028,
            TrainingSystemId = _trainingSystem.Id,
            MajorId = _major.Id,
            MaKhoa = _faculty.MaKhoa
        };
        Assert.IsType<JsonResult>(await WithAjax(students).Create(input));

        var facultyResult = await WithAjax(new FacultiesController(
            _context,
            NullLogger<FacultiesController>.Instance)).DeleteConfirmed(_faculty.MaKhoa);
        var majorResult = await WithAjax(new MajorsController(_context)).DeleteConfirmed(_major.Id);
        var systemResult = await WithAjax(new TrainingSystemsController(_context)).DeleteConfirmed(_trainingSystem.Id);

        Assert.IsType<ConflictObjectResult>(facultyResult);
        Assert.IsType<ConflictObjectResult>(majorResult);
        Assert.IsType<ConflictObjectResult>(systemResult);
        Assert.Single(await _context.Students.ToListAsync());
    }

    [Fact]
    public async Task InvalidAjaxInputReturnsValidationPartialWith422Status()
    {
        var controller = WithAjax(new FacultiesController(
            _context,
            NullLogger<FacultiesController>.Instance));
        controller.ModelState.AddModelError(nameof(Faculty.Name), "Vui lòng nhập tên khoa.");

        var result = Assert.IsType<PartialViewResult>(await controller.Create(new Faculty()));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, controller.Response.StatusCode);
        Assert.Equal("Create", result.ViewName);
    }

    private static T WithAjax<T>(T controller, bool rows = false) where T : Controller
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        if (rows)
        {
            context.Request.QueryString = new QueryString("?partial=rows");
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
