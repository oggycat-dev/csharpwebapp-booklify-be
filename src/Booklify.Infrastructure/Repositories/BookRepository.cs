using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Booklify.Infrastructure.Repositories.Extensions;
using System.Linq.Expressions;

namespace Booklify.Infrastructure.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<(List<Book> Books, int TotalCount)> GetPagedBooksAsync(BookFilterModel filter)
    {
        // Build filter predicate
        Expression<Func<Book, bool>> predicate = BuildFilterPredicate(filter);
        
        // Determine sorting property
        var orderByExpression = GetOrderByExpression(filter.SortBy);

        // Get paged data from base repository with includes for Category
        // Default to descending for CreatedAt (newest first)
        bool isAscending = string.IsNullOrEmpty(filter.SortBy) ? false : filter.IsAscending;
        
        return await GetPagedAsync(
            predicate,
            orderByExpression,
            isAscending,
            filter.PageNumber,
            filter.PageSize,
            b => b.Category,  // Include Category for filtering and response
            b => b.File,      // Include File for additional info
            b => b.Chapters   // Include Chapters for HasChapters check
        );
    }
    
    private Expression<Func<Book, bool>> BuildFilterPredicate(BookFilterModel filter)
    {
        // Start with always true condition
        Expression<Func<Book, bool>> predicate = b => true;
        
        // Apply conditional filters
        if (!string.IsNullOrEmpty(filter.Title))
        {
            predicate = predicate.CombineAnd(b => b.Title.Contains(filter.Title));
        }
        
        if (!string.IsNullOrEmpty(filter.Author))
        {
            predicate = predicate.CombineAnd(b => b.Author.Contains(filter.Author));
        }
        
        if (!string.IsNullOrEmpty(filter.ISBN))
        {
            predicate = predicate.CombineAnd(b => b.ISBN.Contains(filter.ISBN));
        }
        
        if (!string.IsNullOrEmpty(filter.Publisher))
        {
            predicate = predicate.CombineAnd(b => b.Publisher.Contains(filter.Publisher));
        }
        
        if (filter.CategoryId.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.CategoryId == filter.CategoryId.Value);
        }
        
        if (filter.ApprovalStatus.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.ApprovalStatus == filter.ApprovalStatus.Value);
        }
        
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.Status == filter.Status.Value);
        }
        
        if (filter.IsPremium.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.IsPremium == filter.IsPremium.Value);
        }
        
        if (!string.IsNullOrEmpty(filter.Tags))
        {
            predicate = predicate.CombineAnd(b => b.Tags != null && b.Tags.Contains(filter.Tags));
        }
        
        if (filter.HasChapters.HasValue)
        {
            if (filter.HasChapters.Value)
            {
                predicate = predicate.CombineAnd(b => b.Chapters.Any());
            }
            else
            {
                predicate = predicate.CombineAnd(b => !b.Chapters.Any());
            }
        }
        
        if (filter.PublishedDateFrom.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.PublishedDate >= filter.PublishedDateFrom.Value);
        }
        
        if (filter.PublishedDateTo.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.PublishedDate <= filter.PublishedDateTo.Value);
        }
        
        // Global search across all text fields
        if (!string.IsNullOrEmpty(filter.Search))
        {
            predicate = predicate.CombineAnd(b => 
                b.Title.Contains(filter.Search) ||
                b.Author.Contains(filter.Search) ||
                b.ISBN.Contains(filter.Search) ||
                b.Publisher.Contains(filter.Search) ||
                (b.Tags != null && b.Tags.Contains(filter.Search)));
        }
        
        // Rating filters
        if (filter.MinRating.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.AverageRating >= filter.MinRating.Value);
        }
        
        if (filter.MaxRating.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.AverageRating <= filter.MaxRating.Value);
        }
        
        if (filter.MinTotalRatings.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.TotalRatings >= filter.MinTotalRatings.Value);
        }
        
        // Views filters
        if (filter.MinTotalViews.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.TotalViews >= filter.MinTotalViews.Value);
        }
        
        if (filter.MaxTotalViews.HasValue)
        {
            predicate = predicate.CombineAnd(b => b.TotalViews <= filter.MaxTotalViews.Value);
        }
        
        return predicate;
    }

    private Expression<Func<Book, object>> GetOrderByExpression(string? sortBy)
    {
        // Default sort by CreatedAt (newest first) if not specified
        if (string.IsNullOrEmpty(sortBy))
        {
            return b => b.CreatedAt;
        }
        
        // Return appropriate sorting expression based on property name
        return sortBy.ToLower() switch
        {
            "title" => b => b.Title,
            "author" => b => b.Author,
            "isbn" => b => b.ISBN,
            "publisher" => b => b.Publisher,
            "approvalstatus" => b => b.ApprovalStatus,
            "status" => b => b.Status,
            "ispremium" => b => b.IsPremium,
            "pagecount" => b => b.PageCount,
            "publisheddate" => b => b.PublishedDate ?? DateTime.MinValue,
            "createdat" => b => b.CreatedAt,
            "rating" or "averagerating" => b => b.AverageRating,
            "totalratings" => b => b.TotalRatings,
            "totalviews" or "views" => b => b.TotalViews,
            _ => b => b.CreatedAt // Default to CreatedAt for unrecognized properties
        };
    }
}