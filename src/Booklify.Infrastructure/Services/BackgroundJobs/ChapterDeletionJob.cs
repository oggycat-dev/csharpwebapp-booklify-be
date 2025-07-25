using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for handling chapter deletion operations
/// Since chapters are dependent data and have CASCADE delete behavior,
/// we can hard delete them directly for better performance
/// </summary>
public class ChapterDeletionJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ChapterDeletionJob> _logger;

    public ChapterDeletionJob(IServiceScopeFactory serviceScopeFactory, ILogger<ChapterDeletionJob> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Hard delete chapters by book ID in background
    /// Since chapters are dependent data with CASCADE behavior, hard delete is appropriate
    /// </summary>
    [Queue("chapter-deletion")]
    public async Task DeleteChaptersByBookIdAsync(Guid bookId, string userId = "")
    {
        _logger.LogInformation("Starting chapter deletion job for book {BookId} by user {UserId}", bookId, userId);
        
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        try
        {
            await unitOfWork.BeginTransactionAsync();
            
            // Get all chapters for the book (including nested chapters)
            var chapters = await unitOfWork.ChapterRepository.FindAsync(
                c => c.BookId == bookId,
                c => c.Order,
                true // ascending order
            );
            
            var chaptersList = chapters.ToList();
            if (!chaptersList.Any())
            {
                _logger.LogInformation("No chapters found for book {BookId}", bookId);
                await unitOfWork.CommitTransactionAsync();
                return;
            }
            
            _logger.LogInformation("Found {ChapterCount} chapters to delete for book {BookId}", 
                chaptersList.Count, bookId);
            
            // Hard delete all chapters at once for better performance
            await unitOfWork.ChapterRepository.SoftDeleteRangeAsync(chaptersList, userId);
            
            await unitOfWork.CommitTransactionAsync();
            
            _logger.LogInformation("Successfully soft deleted {ChapterCount} chapters for book {BookId}", 
                chaptersList.Count, bookId);
        }
        catch (Exception ex)
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }
            
            _logger.LogError(ex, "Error deleting chapters for book {BookId}", bookId);
            throw;
        }
    }

    /// <summary>
    /// Hard delete specific chapters by IDs in background
    /// </summary>
    [Queue("chapter-deletion")]
    public async Task DeleteChaptersByIdsAsync(List<Guid> chapterIds, string userId = "")
    {
        if (chapterIds == null || !chapterIds.Any())
        {
            _logger.LogWarning("No chapter IDs provided for deletion");
            return;
        }
        
        _logger.LogInformation("Starting chapter deletion job for {ChapterCount} chapters by user {UserId}", 
            chapterIds.Count, userId);
        
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        try
        {
            await unitOfWork.BeginTransactionAsync();
            
            // Get chapters by IDs
            var chapters = await unitOfWork.ChapterRepository.FindAsync(
                c => chapterIds.Contains(c.Id)
            );
            
            var chaptersList = chapters.ToList();
            if (!chaptersList.Any())
            {
                _logger.LogWarning("No chapters found for provided IDs");
                return;
            }
            
            _logger.LogInformation("Found {ChapterCount} chapters to delete", chaptersList.Count);
            
            // Hard delete chapters
            await unitOfWork.ChapterRepository.SoftDeleteRangeAsync(chaptersList, userId);
            
            await unitOfWork.CommitTransactionAsync();
            
            _logger.LogInformation("Successfully deleted {ChapterCount} chapters", chaptersList.Count);
        }
        catch (Exception ex)
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }
            
            _logger.LogError(ex, "Error deleting chapters with IDs: {ChapterIds}", string.Join(", ", chapterIds));
            throw;
        }
    }

    /// <summary>
    /// Cleanup orphaned chapters (chapters whose books are soft deleted)
    /// Run this periodically to clean up chapters of soft-deleted books
    /// </summary>
    [Queue("chapter-deletion")]
    public async Task CleanupOrphanedChaptersAsync(int batchSize = 1000)
    {
        _logger.LogInformation("Starting orphaned chapters cleanup with batch size {BatchSize}", batchSize);
        
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        try
        {
            int totalDeleted = 0;
            bool hasMore = true;
            
            while (hasMore)
            {
                await unitOfWork.BeginTransactionAsync();
                
                // Find chapters whose books are soft deleted
                // Using raw query for better performance with joins
                var orphanedChapters = await unitOfWork.ChapterRepository.FindAsync(
                    c => c.Book.IsDeleted == true,
                    c => c.CreatedAt,
                    true,
                    c => c.Book
                );
                
                var batchChapters = orphanedChapters.Take(batchSize).ToList();
                
                if (!batchChapters.Any())
                {
                    hasMore = false;
                    await unitOfWork.CommitTransactionAsync();
                    break;
                }
                
                _logger.LogInformation("Deleting batch of {ChapterCount} orphaned chapters", batchChapters.Count);
                
                await unitOfWork.ChapterRepository.DeleteRangeAsync(batchChapters);
                await unitOfWork.CommitTransactionAsync();
                
                totalDeleted += batchChapters.Count;
                
                // If we got less than batch size, we're done
                if (batchChapters.Count < batchSize)
                {
                    hasMore = false;
                }
            }
            
            _logger.LogInformation("Orphaned chapters cleanup completed. Total deleted: {TotalDeleted}", totalDeleted);
        }
        catch (Exception ex)
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }
            
            _logger.LogError(ex, "Error during orphaned chapters cleanup");
            throw;
        }
    }
} 