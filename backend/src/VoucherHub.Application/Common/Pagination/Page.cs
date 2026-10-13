namespace VoucherHub.Application.Common.Pagination;
public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
