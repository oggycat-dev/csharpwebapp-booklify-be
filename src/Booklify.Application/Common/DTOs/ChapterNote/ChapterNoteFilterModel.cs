using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.ChapterNote;

/// <summary>
/// Filter model for chapter notes with flexible filtering options
/// </summary>
public class ChapterNoteFilterModel : FilterBase
{
    // Content filters
    public string? Content { get; set; }
    public string? HighlightedText { get; set; }
    public string? Search { get; set; } // Global search across content and highlighted text
    
    // Metadata filters
    public Guid? ChapterId { get; set; }
    public Guid? BookId { get; set; }
    public ChapterNoteType? NoteType { get; set; }
    public string? Color { get; set; }
    public Guid? UserId { get; set; }
    
    // Page filters
    public int? PageNumber_Min { get; set; }
    public int? PageNumber_Max { get; set; }
    public int? SpecificPageNumber { get; set; }
    
    // Status filter (internal use)
    public EntityStatus? Status { get; set; } = EntityStatus.Active;
    
    // Constructors
    public ChapterNoteFilterModel() : base() 
    { 
        IsAscending = false; // Default to newest first for notes
    }
    
    public ChapterNoteFilterModel(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
        IsAscending = false; // Default to newest first for notes
    }
}
