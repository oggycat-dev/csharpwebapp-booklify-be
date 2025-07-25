namespace Booklify.Application.Common.Models;

/// <summary>
/// Model for pagination parameters
/// </summary>
public class PaginationFilter
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    
    public PaginationFilter()
    {
        PageNumber = 1;
        PageSize = 10;
    }
    
    public PaginationFilter(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize > 50 ? 50 : pageSize < 1 ? 10 : pageSize;
    }
} 