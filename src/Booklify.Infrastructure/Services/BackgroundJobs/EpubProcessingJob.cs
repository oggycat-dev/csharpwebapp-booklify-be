using Hangfire;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Entities;

namespace Booklify.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for processing EPUB files
/// </summary>
public class EpubProcessingJob
{
    private readonly ILogger<EpubProcessingJob> _logger;
    private readonly IBooklifyDbContext _dbContext;
    private readonly IEPubService _epubService;

    public EpubProcessingJob(
        ILogger<EpubProcessingJob> logger,
        IBooklifyDbContext dbContext,
        IEPubService epubService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _epubService = epubService;
    }

    /// <summary>
    /// Execute EPUB processing for a book
    /// </summary>
    [Queue("epub-processing")]
    public async Task ExecuteAsync(Guid bookId, string userId = "")
    {
        try
        {
            _logger.LogInformation("Starting EPUB processing job for book: {BookId}", bookId);

            // Get book from database
            var book = await _dbContext.Books.FindAsync(bookId);
            if (book == null)
            {
                _logger.LogWarning("Book not found: {BookId}", bookId);
                return;
            }

            if (string.IsNullOrEmpty(book.FilePath))
            {
                _logger.LogWarning("Book file path is empty for book: {BookId}", bookId);
                return;
            }

            // Extract chapters from EPUB
            var chapters = await _epubService.ExtractChapters(book.FilePath);

            // Save chapters to database
            foreach (var chapter in chapters)
            {
                chapter.BookId = bookId;
                _dbContext.Chapters.Add(chapter);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Completed EPUB processing job for book: {BookId}, extracted {ChapterCount} chapters", 
                bookId, chapters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process EPUB for book: {BookId}", bookId);
            throw;
        }
    }
} 