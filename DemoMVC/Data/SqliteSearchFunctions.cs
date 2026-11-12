using System.Globalization;
using System.Text;

namespace DemoMVC.Data;

public static class SqliteSearchFunctions
{
    public static string? NormalizeForQuery(string? value) =>
        throw new NotSupportedException("This method is only supported inside an EF Core query.");

    public static string? NormalizeNullable(string? value) =>
        value is null ? null : Normalize(value);

    public static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var normalized = char.ToLowerInvariant(character);
            result.Append(normalized == 'đ' ? 'd' : normalized);
        }

        return result.ToString();
    }
}
