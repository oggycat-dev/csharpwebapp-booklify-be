using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

public class ChapterNote : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string? Cfi { get; set; }
    public string? HighlightedText { get; set; }
    public string? Color { get; set; }
    public ChapterNoteType NoteType { get; set; } = ChapterNoteType.TextNote;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    
    // Foreign keys
    public Guid ChapterId { get; set; }
    public Guid UserId { get; set; }
    
    // Navigation properties
    public virtual Chapter Chapter { get; set; } = null!;
    public virtual UserProfile User { get; set; } = null!;
} 