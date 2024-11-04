namespace Jiten.Api.Dtos;

public class PaginatedResponse<T>(T data, int totalItems, int pageSize, int currentOffset)
{
    /// <summary>
    /// The current page data
    /// </summary>
    public T Data { get; set; } = data;

    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    public int TotalItems { get; set; } = totalItems;

    /// <summary>
    /// The number of items per page
    /// </summary>
    public int PageSize { get; set; } = pageSize;

    /// <summary>
    /// The current page offset (0-based)
    /// </summary>
    public int CurrentOffset { get; set; } = currentOffset;
}