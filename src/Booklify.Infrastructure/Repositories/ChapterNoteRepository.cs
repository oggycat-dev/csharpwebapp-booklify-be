using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Persistence;
using Booklify.Infrastructure.Repositories.Extensions;
using System.Linq.Expressions;

namespace Booklify.Infrastructure.Repositories;

public class ChapterNoteRepository : GenericRepository<ChapterNote>, IChapterNoteRepository
{
    public ChapterNoteRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<(List<ChapterNote> Notes, int TotalCount)> GetPagedChapterNotesAsync(ChapterNoteFilterModel filter)
    {
        // Build filter predicate
        Expression<Func<ChapterNote, bool>> predicate = BuildFilterPredicate(filter);
        
        // Determine sorting property
        var orderByExpression = GetOrderByExpression(filter.SortBy);

        // Get paged data from base repository with includes
        // Default to descending for CreatedAt (newest first)
        bool isAscending = string.IsNullOrEmpty(filter.SortBy) ? false : filter.IsAscending;
        
        return await GetPagedAsync(
            predicate,
            orderByExpression,
            isAscending,
            filter.PageNumber,
            filter.PageSize,
            n => n.Chapter!,
            n => n.Chapter!.Book!
        );
    }
    
    private Expression<Func<ChapterNote, bool>> BuildFilterPredicate(ChapterNoteFilterModel filter)
    {
        // Start with always true condition
        Expression<Func<ChapterNote, bool>> predicate = n => true;
        
        // Apply conditional filters
        if (!string.IsNullOrEmpty(filter.Content))
        {
            predicate = predicate.CombineAnd(n => n.Content.Contains(filter.Content));
        }
        
        if (!string.IsNullOrEmpty(filter.HighlightedText))
        {
            predicate = predicate.CombineAnd(n => n.HighlightedText != null && n.HighlightedText.Contains(filter.HighlightedText));
        }
        
        if (!string.IsNullOrEmpty(filter.Color))
        {
            predicate = predicate.CombineAnd(n => n.Color == filter.Color);
        }
        
        if (filter.NoteType.HasValue)
        {
            predicate = predicate.CombineAnd(n => n.NoteType == filter.NoteType.Value);
        }
        
        if (filter.ChapterId.HasValue)
        {
            predicate = predicate.CombineAnd(n => n.ChapterId == filter.ChapterId.Value);
        }
        
        if (filter.BookId.HasValue)
        {
            predicate = predicate.CombineAnd(n => n.Chapter.BookId == filter.BookId.Value);
        }
        
        // Global search across all text fields
        if (!string.IsNullOrEmpty(filter.Search))
        {
            predicate = predicate.CombineAnd(n => 
                n.Content.Contains(filter.Search) ||
                (n.HighlightedText != null && n.HighlightedText.Contains(filter.Search)));
        }
        
        return predicate;
    }

    private Expression<Func<ChapterNote, object>> GetOrderByExpression(string? sortBy)
    {
        // Default sort by CreatedAt (newest first) if not specified
        if (string.IsNullOrEmpty(sortBy))
        {
            return n => n.CreatedAt;
        }
        
        // Return appropriate sorting expression based on property name
        return sortBy.ToLower() switch
        {
            "content" => n => n.Content,
            "pagenumber" => n => n.PageNumber,
            "notetype" => n => n.NoteType,
            "chapterorder" => n => n.Chapter.Order,
            "createdat" => n => n.CreatedAt,
            _ => n => n.CreatedAt // Default to CreatedAt for unrecognized properties
        };
    }
}