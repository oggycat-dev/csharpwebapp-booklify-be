namespace Booklify.Application.Common.Models;

/// <summary>
/// Base filter model with common filtering and sorting properties
/// </summary>
public class FilterBase : PaginationFilter
{
    /// <summary>
    /// Property to sort by (field name)
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Direction of sorting (true for ascending, false for descending)
    /// </summary>
    public bool IsAscending { get; set; } = true;
    
    /// <summary>
    /// Direction of sorting (true for descending, false for ascending)
    /// </summary>
    public bool IsDescending => !IsAscending;
    
    /// <summary>
    /// Search keyword for general text search
    /// </summary>
    public string? SearchKeyword { get; set; }
    
    public FilterBase() : base()
    {
    }
    
    public FilterBase(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
} 