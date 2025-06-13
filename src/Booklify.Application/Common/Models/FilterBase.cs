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
    
    public FilterBase() : base()
    {
    }
    
    public FilterBase(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
} 