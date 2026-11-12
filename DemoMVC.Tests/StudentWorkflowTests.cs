using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;
using Xunit;
using DemoMVC.Controllers;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.ViewModels;

namespace DemoMVC.Tests;

public class StudentWorkflowTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ptpmql-tests-{Guid.NewGuid():N}");
    private string _connectionString = string.Empty;
    private int _facultyId;
    private int _otherFacultyId;
    private int _trainingSystemId;
    private int _otherTrainingSystemId;
    private int _majorId;
    private int _otherMajorId;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_directory, "tests.db"),
            DefaultTimeout = 30,
            ForeignKeys = true,
            Pooling = false
        }.ToString();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");

        var faculty = new Faculty { Name = "Test Faculty" };
        var otherFaculty = new Faculty { Name = "Other Test Faculty" };
        var system = new TrainingSystem { Name = "Test System", StudentCode = "21" };
        var otherSystem = new TrainingSystem { Name = "Other Test System", StudentCode = "22" };
        var major = new Major { Name = "Test Major", AdmissionCode = "7480201", StudentCode = "05" };
        var otherMajor = new Major { Name = "Other Test Major", AdmissionCode = "7460108", StudentCode = "06" };

        context.AddRange(faculty, otherFaculty, system, otherSystem, major, otherMajor);
        await context.SaveChangesAsync();

        _facultyId = faculty.MaKhoa;
        _otherFacultyId = otherFaculty.MaKhoa;
        _trainingSystemId = system.Id;
        _otherTrainingSystemId = otherSystem.Id;
        _majorId = major.Id;
        _otherMajorId = otherMajor.Id;
    }

    public async Task DisposeAsync()
    {
        foreach (var suffix in new[] { "tests.db", "tests.db-shm", "tests.db-wal" })
        {
            var path = Path.Combine(_directory, suffix);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory);
        }

        await Task.CompletedTask;
    }

    [Fact]
    public void BuildStudentNumberUsesExactTenDigitFormat()
    {
        Assert.Equal("2821050199", StudentNumberService.BuildStudentNumber(2028, "21", "05", 199));
        Assert.Equal("2821050200", StudentNumberService.BuildStudentNumber(2028, "21", "05", 200));
    }

    [Fact]
    public async Task CreatePersistsStudentAndNumberAssignment()
    {
        await using var context = CreateContext();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);
        var preview = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);

        var result = await service.CreateAsync(NewInput(preview));

        Assert.Equal(StudentSaveStatus.Saved, result.Status);
        Assert.Equal(preview, result.MaSV);
        Assert.Equal(1, await context.Students.CountAsync());
        var assignment = await context.StudentNumberAssignments.SingleAsync();
        Assert.Equal(preview, assignment.MaSV);
        Assert.Equal(_facultyId, (await context.Students.SingleAsync()).MaKhoa);
    }

    [Fact]
    public async Task SequenceProduces0199Then0200()
    {
        await using var context = CreateContext();
        context.StudentNumberSequences.Add(new StudentNumberSequence
        {
            AdmissionYear = 2028,
            TrainingSystemCode = "21",
            MajorCode = "05",
            LastSequence = 198
        });
        await context.SaveChangesAsync();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);

        var firstCode = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);
        Assert.Equal("2821050199", firstCode);
        Assert.Equal(StudentSaveStatus.Saved, (await service.CreateAsync(NewInput(firstCode))).Status);

        var nextCode = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);
        Assert.Equal("2821050200", nextCode);
        Assert.Equal(StudentSaveStatus.Saved, (await service.CreateAsync(NewInput(nextCode, "Second Student"))).Status);
    }

    [Fact]
    public async Task DifferentMajorAndAdmissionYearHaveIndependentCounters()
    {
        await using var context = CreateContext();
        context.StudentNumberSequences.Add(new StudentNumberSequence
        {
            AdmissionYear = 2028,
            TrainingSystemCode = "21",
            MajorCode = "05",
            LastSequence = 198
        });
        await context.SaveChangesAsync();
        var numbers = new StudentNumberService(context);

        Assert.Equal("2821050199", await numbers.PreviewAsync(2028, _trainingSystemId, _majorId));
        Assert.Equal("2821060001", await numbers.PreviewAsync(2028, _trainingSystemId, _otherMajorId));
        Assert.Equal("2921050001", await numbers.PreviewAsync(2029, _trainingSystemId, _majorId));
        Assert.Equal("2822050001", await numbers.PreviewAsync(2028, _otherTrainingSystemId, _majorId));
    }

    [Fact]
    public async Task StalePreviewDoesNotCreateDuplicateOrConsumeSequence()
    {
        await using var context = CreateContext();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);
        var samePreview = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);

        Assert.Equal(StudentSaveStatus.Saved, (await service.CreateAsync(NewInput(samePreview))).Status);
        var staleResult = await service.CreateAsync(NewInput(samePreview, "Other Student"));

        Assert.Equal(StudentSaveStatus.PreviewChanged, staleResult.Status);
        Assert.Equal("2821050002", staleResult.MaSV);
        Assert.Equal(1, await context.Students.CountAsync());
        var counter = await context.StudentNumberSequences.SingleAsync();
        Assert.Equal(1, counter.LastSequence);
    }

    [Fact]
    public async Task MissingFacultyIsRejectedByServiceAndDatabase()
    {
        await using var context = CreateContext();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);
        var preview = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);
        var input = NewInput(preview);
        input.MaKhoa = int.MaxValue;

        var result = await service.CreateAsync(input);
        Assert.Equal(StudentSaveStatus.FacultyNotFound, result.Status);
        Assert.Empty(await context.Students.ToListAsync());

        context.Students.Add(new StudentRecord
        {
            MaSV = "2821050001",
            FullName = "Invalid Faculty Student",
            Address = "Test address",
            AdmissionYear = 2028,
            TrainingSystemId = _trainingSystemId,
            MajorId = _majorId,
            MaKhoa = int.MaxValue
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FacultyCannotBeDeletedWhileReferenced()
    {
        await CreateStudentAsync();
        await using var context = CreateContext();
        var controller = new FacultiesController(context, NullLogger<FacultiesController>.Instance)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempDataProvider())
        };

        var result = await controller.DeleteConfirmed(_facultyId);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(await context.Faculties.AnyAsync(x => x.MaKhoa == _facultyId));
        Assert.True(await context.Students.AnyAsync(x => x.MaKhoa == _facultyId));
    }

    [Fact]
    public async Task UpdatingStudentChangesFacultyWithoutChangingNumber()
    {
        var created = await CreateStudentAsync();
        await using var context = CreateContext();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);
        var student = await context.Students.SingleAsync(x => x.Id == created.StudentId);
        var input = new StudentFormViewModel
        {
            Id = student.Id,
            MaSV = student.MaSV,
            FullName = "Updated Name",
            Address = "Updated address",
            AdmissionYear = student.AdmissionYear,
            TrainingSystemId = student.TrainingSystemId,
            MajorId = student.MajorId,
            MaKhoa = _otherFacultyId
        };

        var result = await service.UpdateAsync(student.Id, input);

        Assert.Equal(StudentSaveStatus.Saved, result.Status);
        Assert.Equal(created.MaSV, result.MaSV);
        var updated = await context.Students.SingleAsync(x => x.Id == student.Id);
        Assert.Equal(_otherFacultyId, updated.MaKhoa);
        Assert.Equal("Updated Name", updated.FullName);
        Assert.Single(await context.StudentNumberAssignments.ToListAsync());
    }

    [Fact]
    public async Task ChangingMajorAllocatesNewCodeAndKeepsPreviousAssignment()
    {
        var created = await CreateStudentAsync();
        await using var context = CreateContext();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);
        var student = await context.Students.SingleAsync(x => x.Id == created.StudentId);
        var replacementCode = await numbers.PreviewAsync(2028, _trainingSystemId, _otherMajorId);
        var input = new StudentFormViewModel
        {
            Id = student.Id,
            MaSV = replacementCode,
            FullName = student.FullName,
            Address = student.Address,
            AdmissionYear = student.AdmissionYear,
            TrainingSystemId = student.TrainingSystemId,
            MajorId = _otherMajorId,
            MaKhoa = student.MaKhoa
        };

        var result = await service.UpdateAsync(student.Id, input);

        Assert.Equal(StudentSaveStatus.Saved, result.Status);
        Assert.Equal("2821060001", result.MaSV);
        var assignments = await context.StudentNumberAssignments.OrderBy(x => x.MaSV).ToListAsync();
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, x => x.MaSV == created.MaSV);
        Assert.Contains(assignments, x => x.MaSV == result.MaSV);
    }

    [Fact]
    public async Task DeletingStudentDoesNotRewindOrReuseNumber()
    {
        var created = await CreateStudentAsync();
        await using var context = CreateContext();
        var student = await context.Students.SingleAsync(x => x.Id == created.StudentId);
        context.Students.Remove(student);
        await context.SaveChangesAsync();

        Assert.Equal("2821050002", await new StudentNumberService(context).PreviewAsync(2028, _trainingSystemId, _majorId));
        Assert.Null((await context.StudentNumberAssignments.SingleAsync()).StudentId);
        Assert.Equal(created.MaSV, (await context.StudentNumberAssignments.SingleAsync()).MaSV);
    }

    [Fact]
    public async Task ConcurrentCreatesNeverPersistDuplicateNumbers()
    {
        const int requestCount = 12;
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = 0;
        var tasks = Enumerable.Range(0, requestCount).Select(async index =>
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                await using var context = CreateContext();
                var numbers = new StudentNumberService(context);
                var service = new StudentRecordService(context, numbers);
                var preview = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);
                if (attempt == 0 && Interlocked.Increment(ref waiting) == requestCount)
                {
                    ready.TrySetResult(true);
                }

                if (attempt == 0)
                {
                    await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }

                var result = await service.CreateAsync(NewInput(preview, $"Concurrent Student {index}"));
                if (result.Status == StudentSaveStatus.Saved)
                {
                    return result.MaSV!;
                }

                Assert.Equal(StudentSaveStatus.PreviewChanged, result.Status);
            }

            throw new InvalidOperationException("Concurrent create did not converge after retries.");
        });

        var numbersCreated = await Task.WhenAll(tasks);

        Assert.Equal(requestCount, numbersCreated.Distinct(StringComparer.Ordinal).Count());
        await using var verification = CreateContext();
        Assert.Equal(requestCount, await verification.Students.CountAsync());
        Assert.Equal(requestCount, await verification.StudentNumberAssignments.CountAsync());
        Assert.Equal(requestCount, await verification.StudentNumberSequences.SingleAsync().ContinueWith(task => task.Result.LastSequence));
    }

    [Fact]
    public async Task ExhaustsSequenceAt9999AndRejectsNextNumber()
    {
        await using var context = CreateContext();
        context.StudentNumberSequences.Add(new StudentNumberSequence
        {
            AdmissionYear = 2028,
            TrainingSystemCode = "21",
            MajorCode = "05",
            LastSequence = 9998
        });
        await context.SaveChangesAsync();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);

        var lastCode = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);
        Assert.Equal("2821059999", lastCode);
        Assert.Equal(StudentSaveStatus.Saved, (await service.CreateAsync(NewInput(lastCode))).Status);
        await Assert.ThrowsAsync<StudentNumberExhaustedException>(
            () => numbers.PreviewAsync(2028, _trainingSystemId, _majorId));
        await Assert.ThrowsAsync<StudentNumberExhaustedException>(
            () => service.CreateAsync(NewInput(lastCode, "Exhausted Student")));
    }

    [Fact]
    public async Task MissingCatalogCodeAndInvalidYearAreRejected()
    {
        await using var context = CreateContext();
        var system = await context.TrainingSystems.SingleAsync(x => x.Id == _trainingSystemId);
        system.StudentCode = null;
        await context.SaveChangesAsync();
        var numbers = new StudentNumberService(context);

        await Assert.ThrowsAsync<StudentNumberConfigurationException>(
            () => numbers.PreviewAsync(2028, _trainingSystemId, _majorId));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => numbers.PreviewAsync(2100, _trainingSystemId, _majorId));
    }

    [Fact]
    public void RequiredStudentFieldsRejectMissingInput()
    {
        var input = new StudentFormViewModel();
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(input.FullName)));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(input.Address)));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(input.TrainingSystemId)));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(input.MajorId)));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(input.MaKhoa)));
    }

    private async Task<StudentSaveResult> CreateStudentAsync()
    {
        await using var context = CreateContext();
        var numbers = new StudentNumberService(context);
        var service = new StudentRecordService(context, numbers);
        var preview = await numbers.PreviewAsync(2028, _trainingSystemId, _majorId);
        return await service.CreateAsync(NewInput(preview));
    }

    private StudentFormViewModel NewInput(string maSv, string fullName = "Test Student") => new()
    {
        MaSV = maSv,
        FullName = fullName,
        Address = "Test address",
        AdmissionYear = 2028,
        TrainingSystemId = _trainingSystemId,
        MajorId = _majorId,
        MaKhoa = _facultyId
    };

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
