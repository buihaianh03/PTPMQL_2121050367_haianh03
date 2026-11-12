using Microsoft.EntityFrameworkCore;

namespace DemoMVC.ViewModels;

public sealed class PaginatedListViewModel<T>
{
    private PaginatedListViewModel(
        IReadOnlyList<T> items,
        PaginationViewModel pagination)
    {
        Items = items;
        Pagination = pagination;
    }

    public IReadOnlyList<T> Items { get; }
    public PaginationViewModel Pagination { get; }

    public static async Task<PaginatedListViewModel<T>> CreateAsync(
        IQueryable<T> query,
        string? search,
        int page,
        int pageSize)
    {
        var normalizedPageSize = PaginationViewModel.PageSizeOptions.Contains(pageSize)
            ? pageSize
            : 10;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        var currentPage = Math.Clamp(page, 1, Math.Max(totalPages, 1));
        var items = await query
            .Skip((currentPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        return new PaginatedListViewModel<T>(
            items,
            new PaginationViewModel(
                search,
                currentPage,
                normalizedPageSize,
                totalCount,
                totalPages));
    }
}

public sealed class PaginationViewModel
{
    public static IReadOnlyList<int> PageSizeOptions { get; } = [5, 10, 20, 50];

    public PaginationViewModel(
        string? searchTerm,
        int page,
        int pageSize,
        int totalCount,
        int totalPages)
    {
        SearchTerm = searchTerm;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalPages;
    }

    public string? SearchTerm { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public int StartRecord => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int EndRecord => Math.Min(Page * PageSize, TotalCount);

    public IEnumerable<int> VisiblePages
    {
        get
        {
            if (TotalPages == 0)
            {
                return [];
            }

            var start = Math.Max(1, Page - 2);
            var end = Math.Min(TotalPages, Page + 2);
            return Enumerable.Range(start, end - start + 1);
        }
    }
}

public static class ListSearch
{
    public static string? Normalize(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    public static string ContainsPattern(string search)
    {
        var escaped = search
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}
