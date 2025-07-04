using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Services;
using Booklify.Domain.Entities;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Service for EPUB reading progress tracking using CFI (Canonical Fragment Identifier)
/// Specialized for EPUB format with precise position tracking and chapter completion
/// </summary>
public class ReadingProgressService : IReadingProgressService
{
    private readonly ILogger<ReadingProgressService> _logger;
    
    // CFI validation regex pattern
    private static readonly Regex CfiPattern = new Regex(
        @"^epubcfi\(/\d+(\/\d+)*(:\d+)?(\[(.*?)\])?(!(/\d+(\/\d+)*(:\d+)?(\[(.*?)\])?)?)?(/\d+(:\d+)?)?(\[(.*?)\])?(\^|\$|\@\d+)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ReadingProgressService(ILogger<ReadingProgressService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate progress percentage based on CFI position in EPUB
    /// </summary>
    public double CalculateCfiProgressPercentage(string currentCfi, List<Chapter> chapters)
    {
        if (string.IsNullOrEmpty(currentCfi) || !chapters.Any())
            return 0;

        if (!IsValidCfi(currentCfi))
        {
            _logger.LogWarning("Invalid CFI format: {Cfi}", currentCfi);
            return 0;
        }

        try
        {
            // Extract spine position from CFI
            var spinePosition = ExtractSpinePositionFromCfi(currentCfi);
            if (spinePosition <= 0) return 0;

            // Calculate total spine items (chapters + sub-chapters)
            var totalSpineItems = CalculateTotalSpineItems(chapters);
            if (spinePosition >= totalSpineItems) return 100;

            return Math.Round((double)spinePosition / totalSpineItems * 100, 2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate CFI progress for: {Cfi}", currentCfi);
            return 0;
        }
    }

    /// <summary>
    /// Calculate chapter completion percentage based on completed chapters
    /// </summary>
    public double CalculateChapterCompletionPercentage(List<Guid> completedChapterIds, List<Chapter> allChapters)
    {
        if (!allChapters.Any()) return 0;

        // Only count root chapters for fair calculation
        var rootChapters = allChapters.Where(c => c.ParentChapterId == null).ToList();
        if (!rootChapters.Any()) return 0;

        var completedRootChapters = rootChapters.Count(c => completedChapterIds.Contains(c.Id));
        return Math.Round((double)completedRootChapters / rootChapters.Count * 100, 2);
    }

    /// <summary>
    /// Calculate overall progress combining CFI position and chapter completion
    /// </summary>
    public double CalculateOverallProgressPercentage(string currentCfi, List<Guid> completedChapterIds, List<Chapter> chapters)
    {
        if (!chapters.Any()) return 0;

        var cfiProgress = CalculateCfiProgressPercentage(currentCfi, chapters);
        var chapterProgress = CalculateChapterCompletionPercentage(completedChapterIds, chapters);

        // Weighted combination: CFI (60%) + Chapter completion (40%)
        // CFI is more accurate for exact position, chapter completion shows intentional progress
        var overallProgress = (cfiProgress * 0.6) + (chapterProgress * 0.4);

        return Math.Round(overallProgress, 2);
    }

    /// <summary>
    /// Update reading position with new CFI
    /// </summary>
    public async Task<ReadingProgress> UpdateReadingPositionAsync(ReadingProgress progress, string newCfi, 
        Guid? newChapterId = null, int? sessionTimeMinutes = null)
    {
        if (!IsValidCfi(newCfi))
        {
            _logger.LogWarning("Attempted to update with invalid CFI: {Cfi}", newCfi);
            return progress;
        }

        progress.CurrentCfi = newCfi;
        progress.LastReadAt = DateTime.UtcNow;

        if (newChapterId.HasValue)
        {
            progress.CurrentChapterId = newChapterId.Value;
        }

        if (sessionTimeMinutes.HasValue && sessionTimeMinutes.Value > 0)
        {
            progress.TotalReadingTimeMinutes += sessionTimeMinutes.Value;
        }

        // Note: CFI and overall progress calculation would need chapters data
        // This should be called from a higher-level service with chapter access
        
        return progress;
    }

    /// <summary>
    /// Mark chapter as completed and update overall progress
    /// </summary>
    public async Task<ReadingProgress> CompleteChapterAsync(ReadingProgress progress, Guid chapterId, List<Chapter> allChapters)
    {
        var completedChapterIds = GetCompletedChapterIds(progress.CompletedChapterIds);

        // Add chapter if not already completed
        if (!completedChapterIds.Contains(chapterId))
        {
            completedChapterIds.Add(chapterId);
            progress.CompletedChapterIds = SerializeCompletedChapterIds(completedChapterIds);
        }

        // Update current chapter and completion percentage
        progress.CurrentChapterId = chapterId;
        progress.ChapterCompletionPercentage = CalculateChapterCompletionPercentage(completedChapterIds, allChapters);
        
        // Update overall progress if we have CFI
        if (!string.IsNullOrEmpty(progress.CurrentCfi))
        {
            progress.OverallProgressPercentage = CalculateOverallProgressPercentage(
                progress.CurrentCfi, completedChapterIds, allChapters);
        }

        progress.LastReadAt = DateTime.UtcNow;
        
        return progress;
    }

    /// <summary>
    /// Start a new reading session
    /// </summary>
    public ReadingProgress StartReadingSession(ReadingProgress progress)
    {
        progress.SessionStartTime = DateTime.UtcNow;
        return progress;
    }

    /// <summary>
    /// End current reading session and update total reading time
    /// </summary>
    public ReadingProgress EndReadingSession(ReadingProgress progress)
    {
        if (progress.SessionStartTime.HasValue)
        {
            var sessionDuration = DateTime.UtcNow - progress.SessionStartTime.Value;
            var sessionMinutes = (int)sessionDuration.TotalMinutes;
            
            if (sessionMinutes > 0)
            {
                progress.TotalReadingTimeMinutes += sessionMinutes;
            }
            
            progress.SessionStartTime = null;
        }
        
        progress.LastReadAt = DateTime.UtcNow;
        return progress;
    }

    /// <summary>
    /// Validate CFI format for EPUB
    /// </summary>
    public bool IsValidCfi(string cfi)
    {
        if (string.IsNullOrWhiteSpace(cfi))
            return false;

        return CfiPattern.IsMatch(cfi.Trim());
    }

    /// <summary>
    /// Extract chapter ID from CFI if possible
    /// </summary>
    public Guid? ExtractChapterIdFromCfi(string cfi, List<Chapter> chapters)
    {
        if (!IsValidCfi(cfi) || !chapters.Any())
            return null;

        try
        {
            // Look for chapter identifier in CFI
            var match = Regex.Match(cfi, @"\[(.*?)\]");
            if (match.Success)
            {
                var identifier = match.Groups[1].Value;
                
                // Try to find chapter by Href or other identifier
                var chapter = chapters.FirstOrDefault(c => 
                    !string.IsNullOrEmpty(c.Href) && c.Href.Contains(identifier));
                
                return chapter?.Id;
            }

            // Fallback: extract spine position and map to chapter order
            var spinePosition = ExtractSpinePositionFromCfi(cfi);
            if (spinePosition > 0)
            {
                var orderedChapters = chapters.OrderBy(c => c.Order).ToList();
                if (spinePosition <= orderedChapters.Count)
                {
                    return orderedChapters[spinePosition - 1].Id;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract chapter ID from CFI: {Cfi}", cfi);
            return null;
        }
    }

    /// <summary>
    /// Get list of completed chapter IDs from JSON string
    /// </summary>
    public List<Guid> GetCompletedChapterIds(string? completedChapterIdsJson)
    {
        if (string.IsNullOrEmpty(completedChapterIdsJson))
            return new List<Guid>();

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(completedChapterIdsJson) ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize completed chapter IDs: {Json}", completedChapterIdsJson);
            return new List<Guid>();
        }
    }

    /// <summary>
    /// Serialize completed chapter IDs to JSON string
    /// </summary>
    public string SerializeCompletedChapterIds(List<Guid> chapterIds)
    {
        try
        {
            return JsonSerializer.Serialize(chapterIds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize completed chapter IDs");
            return "[]";
        }
    }

    /// <summary>
    /// Get reading statistics for analytics
    /// </summary>
    public async Task<ReadingProgressStats> GetReadingStatsAsync(ReadingProgress progress, List<Chapter> chapters)
    {
        var completedChapterIds = GetCompletedChapterIds(progress.CompletedChapterIds);
        var rootChapters = chapters.Where(c => c.ParentChapterId == null).ToList();
        
        var stats = new ReadingProgressStats
        {
            OverallProgress = progress.OverallProgressPercentage,
            CfiProgress = progress.CfiProgressPercentage,
            ChapterProgress = progress.ChapterCompletionPercentage,
            CompletedChapters = completedChapterIds.Count(id => rootChapters.Any(c => c.Id == id)),
            TotalChapters = rootChapters.Count,
            TotalReadingTimeMinutes = progress.TotalReadingTimeMinutes,
            LastReadAt = progress.LastReadAt
        };

        // Get current chapter title
        if (progress.CurrentChapterId.HasValue)
        {
            var currentChapter = chapters.FirstOrDefault(c => c.Id == progress.CurrentChapterId.Value);
            stats.CurrentChapterTitle = currentChapter?.Title ?? "";
        }

        // Estimate time to complete (rough calculation)
        if (stats.OverallProgress > 0 && stats.TotalReadingTimeMinutes > 0)
        {
            var estimatedTotalTime = (stats.TotalReadingTimeMinutes / stats.OverallProgress) * 100;
            stats.EstimatedTimeToCompleteMinutes = (int)(estimatedTotalTime - stats.TotalReadingTimeMinutes);
        }

        return stats;
    }

    /// <summary>
    /// Extract spine position from CFI (simplified implementation)
    /// </summary>
    private int ExtractSpinePositionFromCfi(string cfi)
    {
        try
        {
            // CFI format example: epubcfi(/6/4[chapter1]!/4/2)
            // The first number after the initial '/' is the spine position
            var match = Regex.Match(cfi, @"epubcfi\(/(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var position))
            {
                // CFI spine indices are even numbers (2, 4, 6, ...), convert to 1-based position
                return position / 2;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Calculate total spine items for progress calculation
    /// </summary>
    private int CalculateTotalSpineItems(List<Chapter> chapters)
    {
        // For simplicity, count all chapters as spine items
        // In a real implementation, this would be based on the EPUB manifest
        return chapters.Count;
    }
} 