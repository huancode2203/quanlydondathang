using System.ComponentModel.DataAnnotations;

namespace Sv.Order.DTOs;

// Request DTOs describe the public API; EF entities describe persisted database rows.
public class SearchRequest
{
    [StringLength(200, ErrorMessage = "Từ khóa không được quá 200 ký tự.")]
    public string? Keyword { get; init; }
}

public class PagedFilterRequest : SearchRequest
{
    [Range(1, 1_000_000, ErrorMessage = "Trang phải từ 1 đến 1.000.000.")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Số dòng mỗi trang phải từ 1 đến 100.")]
    public int PageSize { get; init; } = 10;
}

public sealed class EmptyRequest;

public sealed class IdRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã định danh phải là số nguyên dương hợp lệ.")]
    public int Id { get; init; }
}

public sealed class OrderIdRequest
{
    [Range(1L, long.MaxValue, ErrorMessage = "Mã đơn hàng phải là số nguyên dương hợp lệ.")]
    public long Id { get; init; }
}
