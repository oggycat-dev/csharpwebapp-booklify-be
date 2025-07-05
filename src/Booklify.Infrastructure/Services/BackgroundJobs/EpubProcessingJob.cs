using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Entities;
using Booklify.Domain.Commons;

namespace Booklify.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job for processing EPUB files
/// </summary>
public class EpubProcessingJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EpubProcessingJob> _logger;

    public EpubProcessingJob(IServiceScopeFactory serviceScopeFactory, ILogger<EpubProcessingJob> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Execute EPUB processing for a book using pre-downloaded file content
    /// </summary>
    [Queue("epub-processing")]
    public async Task ExecuteAsync(Guid bookId, string userId, byte[] fileContent, string fileExtension)
    {
        _logger.LogInformation("Starting EPUB processing job for book {BookId} by user {UserId} with pre-downloaded content", bookId, userId);
        
        // Tạo scope để lấy các service cần thiết
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var epubService = scope.ServiceProvider.GetRequiredService<IEPubService>();
        
        try
        {
            // 1. Lấy book từ database để validate
            var book = await unitOfWork.BookRepository.GetFirstOrDefaultAsync(
                b => b.Id == bookId
            );
        
            if (book == null)
            {
                _logger.LogError("Book not found for bookId: {BookId}", bookId);
                return;
            }
            
            // 2. Tạo temp file từ file content đã có
            var tempFilePath = await CreateTempFileFromContent(fileContent, fileExtension);
            try
            {
                // 3. Extract chapters only (metadata already extracted during book creation)
                _logger.LogInformation("Starting chapter extraction for book {BookId}", bookId);
                var chapters = await epubService.ExtractChapters(tempFilePath);
                _logger.LogInformation("Extracted {ChapterCount} chapters from EPUB for book {BookId}", chapters.Count, bookId);

                // 4. Begin transaction to save chapters
                _logger.LogInformation("Starting database transaction for book {BookId}", bookId);
                await unitOfWork.BeginTransactionAsync();

                // 5. Map chapters với BookId
                foreach (var chapter in chapters)
                {
                    chapter.BookId = bookId;
                }

                // 6. Save chapters to database
                await unitOfWork.ChapterRepository.AddRangeAsync(chapters);
                await unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Completed EPUB processing job for book: {BookId}, extracted {ChapterCount} chapters", 
                    bookId, chapters.Count);
            }
            catch (Exception ex)
            {
                // Rollback transaction if it was started
                try
                {
                    await unitOfWork.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback transaction");
                }
                
                _logger.LogError(ex, "Error processing EPUB for bookId: {BookId}", bookId);
                throw;
            }
            finally
            {
                // Xóa temp file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing EPUB for bookId: {BookId}", bookId);
            throw;
        }
    }

    /// <summary>
    /// Execute EPUB processing for a book (legacy method - downloads file from storage)
    /// </summary>
    [Queue("epub-processing")]
    public async Task ExecuteAsync(Guid bookId, string userId)
    {
        _logger.LogInformation("Starting EPUB processing job for book {BookId} by user {UserId}", bookId, userId);
        
        // Tạo scope để lấy các service cần thiết
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var epubService = scope.ServiceProvider.GetRequiredService<IEPubService>();
        
        try
        {
            // 1. Lấy book từ database
            var book = await unitOfWork.BookRepository.GetFirstOrDefaultAsync(
                b => b.Id == bookId,
                b => b.File!
            );
        
            if (book == null || book.File == null)
            {
                _logger.LogError("Book or file not found for bookId: {BookId}", bookId);
                return;
            }
            
            // 2. Download file EPUB từ storage
            var fileStream = await storageService.DownloadFileAsync(book.File.FilePath);
            if (fileStream == null)
            {
                _logger.LogError("Failed to download file for bookId: {BookId}", bookId);
                return;
            }
            
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }
            
            // 3. Tạo temp file để VersOne.Epub có thể đọc
            var tempFilePath = await CreateTempFileFromContent(fileContent, book.File.Extension);
            try
            {
                // 4. Extract chapters only (metadata already extracted during book creation)
                _logger.LogInformation("Starting chapter extraction for book {BookId}", bookId);
                var chapters = await epubService.ExtractChapters(tempFilePath);
                _logger.LogInformation("Extracted {ChapterCount} chapters from EPUB for book {BookId}", chapters.Count, bookId);

                // 5. Begin transaction to save chapters
                _logger.LogInformation("Starting database transaction for book {BookId}", bookId);
                await unitOfWork.BeginTransactionAsync();

                // 6. Map chapters với BookId
                foreach (var chapter in chapters)
                {
                    chapter.BookId = bookId;
                }

                // 7. Save chapters to database
                await unitOfWork.ChapterRepository.AddRangeAsync(chapters);
                await unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Completed EPUB processing job for book: {BookId}, extracted {ChapterCount} chapters", 
                    bookId, chapters.Count);
            }
            catch (Exception ex)
            {
                // Rollback transaction if it was started
                try
                {
                    await unitOfWork.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback transaction");
                }
                
                _logger.LogError(ex, "Error processing EPUB for bookId: {BookId}", bookId);
                throw;
            }
            finally
            {
                // Xóa temp file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing EPUB for bookId: {BookId}", bookId);
            throw;
        }
    }

    private async Task<string> CreateTempFileFromContent(byte[] content, string extension)
    {
        var tempFilePath = Path.GetTempFileName();
        tempFilePath = Path.ChangeExtension(tempFilePath, extension);

        await File.WriteAllBytesAsync(tempFilePath, content);
        return tempFilePath;
    }
} 