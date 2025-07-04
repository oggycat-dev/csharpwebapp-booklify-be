using Booklify.Domain.Entities;

namespace Booklify.Application.Common.Services;

/// <summary>
/// Service for EPUB reading progress tracking using CFI (Canonical Fragment Identifier)
/// </summary>
public interface IReadingProgressService
{
    /// <summary>
    /// Calculate progress percentage based on CFI position in EPUB
    /// </summary>
    double CalculateCfiProgressPercentage(string currentCfi, List<Chapter> chapters);
    
    /// <summary>
    /// Calculate chapter completion percentage
    /// </summary>
    double CalculateChapterCompletionPercentage(List<Guid> completedChapterIds, List<Chapter> allChapters);
    
    /// <summary>
    /// Calculate overall progress combining CFI position and chapter completion
    /// </summary>
    double CalculateOverallProgressPercentage(string currentCfi, List<Guid> completedChapterIds, List<Chapter> chapters);
    
    /// <summary>
    /// Update reading position with new CFI
    /// </summary>
    Task<ReadingProgress> UpdateReadingPositionAsync(ReadingProgress progress, string newCfi, 
        Guid? newChapterId = null, int? sessionTimeMinutes = null);
    
    /// <summary>
    /// Mark chapter as completed and update overall progress
    /// </summary>
    Task<ReadingProgress> CompleteChapterAsync(ReadingProgress progress, Guid chapterId, List<Chapter> allChapters);
    
    /// <summary>
    /// Start a new reading session
    /// </summary>
    ReadingProgress StartReadingSession(ReadingProgress progress);
    
    /// <summary>
    /// End current reading session and update total reading time
    /// </summary>
    ReadingProgress EndReadingSession(ReadingProgress progress);
    
    /// <summary>
    /// Validate CFI format for EPUB
    /// </summary>
    bool IsValidCfi(string cfi);
    
    /// <summary>
    /// Extract chapter ID from CFI if possible
    /// </summary>
    Guid? ExtractChapterIdFromCfi(string cfi, List<Chapter> chapters);
    
    /// <summary>
    /// Get list of completed chapter IDs from JSON string
    /// </summary>
    List<Guid> GetCompletedChapterIds(string? completedChapterIdsJson);
    
    /// <summary>
    /// Serialize completed chapter IDs to JSON string
    /// </summary>
    string SerializeCompletedChapterIds(List<Guid> chapterIds);
    
    /// <summary>
    /// Get reading statistics for analytics
    /// </summary>
    Task<ReadingProgressStats> GetReadingStatsAsync(ReadingProgress progress, List<Chapter> chapters);
}

/// <summary>
/// Reading progress statistics for analytics
/// </summary>
public class ReadingProgressStats
{
    public double OverallProgress { get; set; }
    public double CfiProgress { get; set; }
    public double ChapterProgress { get; set; }
    public int CompletedChapters { get; set; }
    public int TotalChapters { get; set; }
    public int TotalReadingTimeMinutes { get; set; }
    public DateTime LastReadAt { get; set; }
    public string CurrentChapterTitle { get; set; } = string.Empty;
    public int EstimatedTimeToCompleteMinutes { get; set; }
} 