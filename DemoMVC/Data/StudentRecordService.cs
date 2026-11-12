using System.Data;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;
using DemoMVC.ViewModels;

namespace DemoMVC.Data;

public enum StudentSaveStatus
{
    Saved,
    NotFound,
    FacultyNotFound,
    TrainingSystemNotFound,
    MajorNotFound,
    PreviewChanged,
    PreviewRequired
}

public sealed record StudentSaveResult(StudentSaveStatus Status, string? MaSV = null, int? StudentId = null);

public class StudentRecordService
{
    private readonly ApplicationDbContext _context;
    private readonly StudentNumberService _numberService;

    public StudentRecordService(ApplicationDbContext context, StudentNumberService numberService)
    {
        _context = context;
        _numberService = numberService;
    }

    public async Task<StudentSaveResult> CreateAsync(StudentFormViewModel input)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var references = await LoadReferencesAsync(input);
        if (references.Result is not null)
        {
            return references.Result;
        }

        var trainingSystem = references.TrainingSystem!;
        var major = references.Major!;
        var faculty = references.Faculty!;
        if (string.IsNullOrWhiteSpace(input.MaSV))
        {
            return new StudentSaveResult(StudentSaveStatus.PreviewRequired);
        }

        var sequence = await _numberService.AllocateNextSequenceAsync(
            input.AdmissionYear,
            trainingSystem.StudentCode!,
            major.StudentCode!,
            transaction);
        var maSv = StudentNumberService.BuildStudentNumber(
            input.AdmissionYear,
            trainingSystem.StudentCode!,
            major.StudentCode!,
            sequence);

        if (!string.Equals(maSv, input.MaSV, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync();
            var currentPreview = await _numberService.PreviewAsync(
                input.AdmissionYear,
                trainingSystem.Id,
                major.Id);
            return new StudentSaveResult(StudentSaveStatus.PreviewChanged, currentPreview);
        }

        var student = new StudentRecord
        {
            MaSV = maSv,
            FullName = input.FullName.Trim(),
            Address = input.Address.Trim(),
            AdmissionYear = input.AdmissionYear,
            TrainingSystemId = trainingSystem.Id,
            MajorId = major.Id,
            MaKhoa = faculty.MaKhoa
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        _context.StudentNumberAssignments.Add(new StudentNumberAssignment
        {
            MaSV = maSv,
            AdmissionYear = input.AdmissionYear,
            TrainingSystemCode = trainingSystem.StudentCode!,
            MajorCode = major.StudentCode!,
            SequenceNumber = sequence,
            StudentId = student.Id,
            AssignedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new StudentSaveResult(StudentSaveStatus.Saved, maSv, student.Id);
    }

    public async Task<StudentSaveResult> UpdateAsync(int id, StudentFormViewModel input)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var student = await _context.Students.SingleOrDefaultAsync(x => x.Id == id);
        if (student is null)
        {
            return new StudentSaveResult(StudentSaveStatus.NotFound);
        }

        var references = await LoadReferencesAsync(input);
        if (references.Result is not null)
        {
            return references.Result;
        }

        var trainingSystem = references.TrainingSystem!;
        var major = references.Major!;
        var faculty = references.Faculty!;
        var numberDimensionsChanged =
            student.AdmissionYear != input.AdmissionYear
            || student.TrainingSystemId != trainingSystem.Id
            || student.MajorId != major.Id;

        var newMaSv = student.MaSV;
        var sequence = 0;
        if (numberDimensionsChanged)
        {
            if (string.IsNullOrWhiteSpace(input.MaSV))
            {
                return new StudentSaveResult(StudentSaveStatus.PreviewRequired);
            }

            sequence = await _numberService.AllocateNextSequenceAsync(
                input.AdmissionYear,
                trainingSystem.StudentCode!,
                major.StudentCode!,
                transaction);
            newMaSv = StudentNumberService.BuildStudentNumber(
                input.AdmissionYear,
                trainingSystem.StudentCode!,
                major.StudentCode!,
                sequence);

            if (!string.Equals(newMaSv, input.MaSV, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync();
                var currentPreview = await _numberService.PreviewAsync(
                    input.AdmissionYear,
                    trainingSystem.Id,
                    major.Id);
                return new StudentSaveResult(StudentSaveStatus.PreviewChanged, currentPreview);
            }
        }

        student.MaSV = newMaSv;
        student.FullName = input.FullName.Trim();
        student.Address = input.Address.Trim();
        student.AdmissionYear = input.AdmissionYear;
        student.TrainingSystemId = trainingSystem.Id;
        student.MajorId = major.Id;
        student.MaKhoa = faculty.MaKhoa;
        await _context.SaveChangesAsync();

        if (numberDimensionsChanged)
        {
            _context.StudentNumberAssignments.Add(new StudentNumberAssignment
            {
                MaSV = newMaSv,
                AdmissionYear = input.AdmissionYear,
                TrainingSystemCode = trainingSystem.StudentCode!,
                MajorCode = major.StudentCode!,
                SequenceNumber = sequence,
                StudentId = student.Id,
                AssignedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return new StudentSaveResult(StudentSaveStatus.Saved, student.MaSV, student.Id);
    }

    private async Task<(Faculty? Faculty, TrainingSystem? TrainingSystem, Major? Major, StudentSaveResult? Result)>
        LoadReferencesAsync(StudentFormViewModel input)
    {
        var faculty = await _context.Faculties.SingleOrDefaultAsync(x => x.MaKhoa == input.MaKhoa);
        if (faculty is null)
        {
            return (null, null, null, new StudentSaveResult(StudentSaveStatus.FacultyNotFound));
        }

        var trainingSystem = await _context.TrainingSystems
            .SingleOrDefaultAsync(x => x.Id == input.TrainingSystemId);
        if (trainingSystem is null)
        {
            return (faculty, null, null, new StudentSaveResult(StudentSaveStatus.TrainingSystemNotFound));
        }

        var major = await _context.Majors.SingleOrDefaultAsync(x => x.Id == input.MajorId);
        if (major is null)
        {
            return (faculty, trainingSystem, null, new StudentSaveResult(StudentSaveStatus.MajorNotFound));
        }

        return (faculty, trainingSystem, major, null);
    }
}
