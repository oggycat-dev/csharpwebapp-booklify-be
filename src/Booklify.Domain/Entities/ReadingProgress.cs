using Booklify.Domain.Commons;

namespace Booklify.Domain.Entities;

/// <summary>
/// Simple reading progress tracking based on chapter access
/// </summary>
public class ReadingProgress : BaseEntity
{
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    
    // Current position (latest accessed chapter)
    public Guid? CurrentChapterId { get; set; } // Latest chapter user accessed
    
    // Book-level progress (calculated by backend from chapter count)
    //public double OverallProgressPercentage { get; set; } = 0; // read_chapters / total_chapters * 100
    public int CompletedChaptersCount { get; set; } = 0;
    // Book-level tracking
    public DateTime LastReadAt { get; set; } // Last time user accessed any chapter
    public DateTime? FirstReadAt { get; set; } // When user first opened this book
    
    public bool IsCompleted { get; set; } = false;
    // Navigation Properties
    public virtual Book Book { get; set; } = null!;
    public virtual UserProfile User { get; set; } = null!;
    public virtual Chapter? CurrentChapter { get; set; }
    public virtual ICollection<ChapterReadingProgress> ChapterProgresses { get; set; } = new List<ChapterReadingProgress>();
}