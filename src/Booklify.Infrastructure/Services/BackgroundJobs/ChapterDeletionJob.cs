using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Enums;

namespace Booklify.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for handling chapter deletions
/// </summary>
public class ChapterDeletionJob
{
    private readonly ILogger<ChapterDeletionJob> _logger;
    private readonly IBooklifyDbContext _dbContext;

    public ChapterDeletionJob(
        ILogger<ChapterDeletionJob> logger,
        IBooklifyDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Delete chapters by book ID
    /// </summary>
    [Queue("epub-processing")]
    public async Task DeleteChaptersByBookIdAsync(Guid bookId, string userId = "")
    {
        try
        {
            _logger.LogInformation("Starting chapter deletion job for book: {BookId}", bookId);

            var chapters = await _dbContext.Chapters
                .Where(c => c.BookId == bookId)
                .ToListAsync();

            foreach (var chapter in chapters)
            {
                chapter.Status = EntityStatus.Deleted;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Completed chapter deletion job for book: {BookId}, deleted {ChapterCount} chapters", 
                bookId, chapters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chapters for book: {BookId}", bookId);
            throw;
        }
    }

    /// <summary>
    /// Delete chapters by IDs
    /// </summary>
    [Queue("epub-processing")]
    public async Task DeleteChaptersByIdsAsync(List<Guid> chapterIds, string userId = "")
    {
        try
        {
            _logger.LogInformation("Starting chapter deletion job for {ChapterCount} chapters", chapterIds.Count);

            var chapters = await _dbContext.Chapters
                .Where(c => chapterIds.Contains(c.Id))
                .ToListAsync();

            foreach (var chapter in chapters)
            {
                chapter.Status = EntityStatus.Deleted;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Completed chapter deletion job for {ChapterCount} chapters", chapters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {ChapterCount} chapters", chapterIds.Count);
            throw;
        }
    }

    /// <summary>
    /// Cleanup orphaned chapters (chapters without valid books)
    /// </summary>
    [Queue("epub-processing")]
    public async Task CleanupOrphanedChaptersAsync(int batchSize = 1000)
    {
        try
        {
            _logger.LogInformation("Starting orphaned chapter cleanup with batch size: {BatchSize}", batchSize);

            var orphanedChapters = await _dbContext.Chapters
                .Where(c => c.BookId == null || !_dbContext.Books.Any(b => b.Id == c.BookId))
                .Take(batchSize)
                .ToListAsync();

            foreach (var chapter in orphanedChapters)
            {
                chapter.Status = EntityStatus.Deleted;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Completed orphaned chapter cleanup, processed {ChapterCount} chapters", 
                orphanedChapters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup orphaned chapters");
            throw;
        }
    }
} 