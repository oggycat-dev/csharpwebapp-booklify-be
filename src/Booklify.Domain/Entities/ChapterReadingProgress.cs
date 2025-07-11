using Booklify.Domain.Commons;

namespace Booklify.Domain.Entities;

/// <summary>
/// Chapter reading tracking with current CFI position and completion status
/// </summary>
public class ChapterReadingProgress : BaseEntity
{
    public Guid ReadingProgressId { get; set; } // FK to ReadingProgress
    public Guid ChapterId { get; set; } // FK to Chapter
    
    // Chapter reading position and completion
    public string? CurrentCfi { get; set; } // Current CFI position in this chapter (frontend can detect start/end)
    public bool IsCompleted { get; set; } = false; // Chapter completion determined by frontend
    public DateTime? CompletedAt { get; set; } // When chapter was marked completed by frontend
    
    // Access tracking
    public DateTime LastReadAt { get; set; } // When was this chapter last accessed
    
    // Navigation Properties
    public virtual ReadingProgress ReadingProgress { get; set; } = null!;
    public virtual Chapter Chapter { get; set; } = null!;
}


