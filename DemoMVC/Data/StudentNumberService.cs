using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using DemoMVC.Models;

namespace DemoMVC.Data;

public sealed class StudentNumberConfigurationException(string message) : Exception(message);

public sealed class StudentNumberExhaustedException : Exception
{
    public StudentNumberExhaustedException()
        : base("Tổ hợp năm nhập học, hệ đào tạo và ngành học đã dùng hết 9999 số thứ tự.")
    {
    }
}

public class StudentNumberService
{
    private readonly ApplicationDbContext _context;

    public StudentNumberService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> PreviewAsync(int admissionYear, int trainingSystemId, int majorId)
    {
        ValidateYear(admissionYear);
        var (trainingCode, majorCode) = await GetConfiguredCodesAsync(trainingSystemId, majorId);

        var lastSequence = await _context.StudentNumberSequences
            .Where(x => x.AdmissionYear == admissionYear
                && x.TrainingSystemCode == trainingCode
                && x.MajorCode == majorCode)
            .Select(x => (int?)x.LastSequence)
            .SingleOrDefaultAsync() ?? 0;

        if (lastSequence >= 9999)
        {
            throw new StudentNumberExhaustedException();
        }

        return BuildStudentNumber(admissionYear, trainingCode, majorCode, lastSequence + 1);
    }

    public async Task<int> AllocateNextSequenceAsync(
        int admissionYear,
        string trainingCode,
        string majorCode,
        IDbContextTransaction transaction)
    {
        ValidateYear(admissionYear);
        if (!IsValidCode(trainingCode) || !IsValidCode(majorCode))
        {
            throw new StudentNumberConfigurationException("Mã hệ và mã ngành phải được cấu hình đúng 2 chữ số.");
        }

        var connection = (SqliteConnection)_context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction.GetDbTransaction();
        command.CommandText = """
            INSERT INTO "StudentNumberSequences"
                ("AdmissionYear", "TrainingSystemCode", "MajorCode", "LastSequence")
            VALUES
                ($year, $trainingCode, $majorCode, 1)
            ON CONFLICT ("AdmissionYear", "TrainingSystemCode", "MajorCode")
            DO UPDATE SET "LastSequence" = "StudentNumberSequences"."LastSequence" + 1
            WHERE "StudentNumberSequences"."LastSequence" < 9999
            RETURNING "LastSequence";
            """;
        command.Parameters.AddWithValue("$year", admissionYear);
        command.Parameters.AddWithValue("$trainingCode", trainingCode);
        command.Parameters.AddWithValue("$majorCode", majorCode);

        var value = await command.ExecuteScalarAsync();
        if (value is null or DBNull)
        {
            throw new StudentNumberExhaustedException();
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public static string BuildStudentNumber(int admissionYear, string trainingCode, string majorCode, int sequence)
    {
        ValidateYear(admissionYear);
        if (!IsValidCode(trainingCode) || !IsValidCode(majorCode))
        {
            throw new StudentNumberConfigurationException("Mã hệ và mã ngành phải được cấu hình đúng 2 chữ số.");
        }

        if (sequence is < 1 or > 9999)
        {
            throw new StudentNumberExhaustedException();
        }

        return string.Concat(
            (admissionYear % 100).ToString("D2", CultureInfo.InvariantCulture),
            trainingCode,
            majorCode,
            sequence.ToString("D4", CultureInfo.InvariantCulture));
    }

    private async Task<(string TrainingCode, string MajorCode)> GetConfiguredCodesAsync(
        int trainingSystemId,
        int majorId)
    {
        var trainingCode = await _context.TrainingSystems
            .Where(x => x.Id == trainingSystemId)
            .Select(x => x.StudentCode)
            .SingleOrDefaultAsync();
        var majorCode = await _context.Majors
            .Where(x => x.Id == majorId)
            .Select(x => x.StudentCode)
            .SingleOrDefaultAsync();

        if (!IsValidCode(trainingCode) || !IsValidCode(majorCode))
        {
            throw new StudentNumberConfigurationException(
                "Hệ đào tạo hoặc ngành học chưa được cấu hình mã sinh viên 2 chữ số.");
        }

        return (trainingCode!, majorCode!);
    }

    private static bool IsValidCode(string? code) =>
        code is { Length: 2 } && code.All(char.IsAsciiDigit);

    private static void ValidateYear(int year)
    {
        if (year is < 2000 or > 2099)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Năm nhập học phải từ 2000 đến 2099.");
        }
    }
}
