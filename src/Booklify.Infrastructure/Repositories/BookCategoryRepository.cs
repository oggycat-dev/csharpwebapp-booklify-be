using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Infrastructure.Repositories.Extensions;
using System.Linq.Expressions;

namespace Booklify.Infrastructure.Repositories;

public class BookCategoryRepository : GenericRepository<BookCategory>, IBookCategoryRepository
{
    public BookCategoryRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<(List<BookCategory> BookCategories, int TotalCount)> GetPagedBookCategoriesAsync(BookCategoryFilterModel filter)
    {
        // Build filter predicate
        Expression<Func<BookCategory, bool>> predicate = BuildFilterPredicate(filter);
        
        // Determine sorting property
        var orderByExpression = GetOrderByExpression(filter.SortBy);

        // Get paged data from base repository with includes for Books count
        return await GetPagedAsync(
            predicate,
            orderByExpression,
            filter.IsAscending,
            filter.PageNumber,
            filter.PageSize,
            bc => bc.Books  // Include Books for count
        );
    }
    
    private Expression<Func<BookCategory, bool>> BuildFilterPredicate(BookCategoryFilterModel filter)
    {
        // Start with always true condition
        Expression<Func<BookCategory, bool>> predicate = bc => true;
        
        // Apply conditional filters
        if (!string.IsNullOrEmpty(filter.Name))
        {
            predicate = predicate.CombineAnd(bc => bc.Name.Contains(filter.Name));
        }
        
        if (!string.IsNullOrEmpty(filter.Description))
        {
            predicate = predicate.CombineAnd(bc => bc.Description.Contains(filter.Description));
        }
        
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(bc => bc.Status == filter.Status);
        }
        
        return predicate;
    }

    private Expression<Func<BookCategory, object>> GetOrderByExpression(string? sortBy)
    {
        // Default sort by name if not specified
        if (string.IsNullOrEmpty(sortBy))
        {
            return bc => bc.Name;
        }
        
        // Return appropriate sorting expression based on property name
        return sortBy.ToLower() switch
        {
            "name" => bc => bc.Name,
            "description" => bc => bc.Description,
            "status" => bc => bc.Status,
            "createdat" => bc => bc.CreatedAt,
            "bookscount" => bc => bc.Books!.Count,
            _ => bc => bc.Name // Default to name for unrecognized properties
        };
    }
} 