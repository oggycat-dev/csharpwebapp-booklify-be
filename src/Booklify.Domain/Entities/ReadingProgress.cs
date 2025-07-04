using Booklify.Domain.Commons;

namespace Booklify.Domain.Entities;

/// <summary>
/// Reading progress for EPUB books using CFI (Canonical Fragment Identifier) standard
/// </summary>
public class ReadingProgress : BaseEntity
{
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    
    // EPUB CFI-based position tracking
    public string CurrentCfi { get; set; } = string.Empty; // Current precise position in EPUB
    public Guid? CurrentChapterId { get; set; } // Current chapter being read
    
    // Chapter completion tracking
    public string? CompletedChapterIds { get; set; } // JSON array of completed chapter IDs
    public double ChapterCompletionPercentage { get; set; } = 0; // % based on completed chapters
    
    // CFI-based reading progress
    public double CfiProgressPercentage { get; set; } = 0; // % based on CFI position
    
    // Reading session tracking
    public int TotalReadingTimeMinutes { get; set; } = 0; // Total time spent reading
    public DateTime LastReadAt { get; set; }
    public DateTime? SessionStartTime { get; set; } // Current session start time
    
    // Overall progress (combining CFI + chapter completion)
    public double OverallProgressPercentage { get; set; } = 0; // Smart combined percentage

    //Navigation Properties
    public virtual Book Book { get; set; } = null!;
    public virtual UserProfile User { get; set; } = null!;
    public virtual Chapter? CurrentChapter { get; set; }
}