namespace UAF.Application.Common.Models;
public sealed record PaginationMetadata(int Page, int PageSize, int TotalCount, int TotalPages)
{
    public static PaginationMetadata Create(int page, int pageSize, int totalCount)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize));
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount));
        return new(page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
    public int Skip => checked((Page - 1) * PageSize);
}
