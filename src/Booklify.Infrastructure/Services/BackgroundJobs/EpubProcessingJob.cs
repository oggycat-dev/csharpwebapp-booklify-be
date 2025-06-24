using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Models;
using Booklify.Infrastructure.Utils;
using VersOne.Epub;

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
    /// Execute EPUB processing for a book
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
                b => b.File
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
                // 4. Extract chapters và metadata trước (outside transaction)
                var chapters = await epubService.ExtractChapters(tempFilePath);
                var metadata = await ExtractMetadata(tempFilePath);

                // 5. Upload cover image nếu có (outside transaction)
                string? coverImageUrl = null;
                if (metadata.CoverImageBytes != null && metadata.CoverImageBytes.Length > 0)
                {
                    coverImageUrl = await UploadCoverImage(metadata.CoverImageBytes, bookId, storageService);
                }

                // 6. Begin transaction sau khi tất cả external operations hoàn thành
                await unitOfWork.BeginTransactionAsync();

                // 7. Map chapters với BookId
                foreach (var chapter in chapters)
                {
                    chapter.BookId = bookId;
                }

                // 8. Update book với metadata
                if (!string.IsNullOrWhiteSpace(metadata.Author))
                {
                    book.Author = metadata.Author;
                }
                if (!string.IsNullOrWhiteSpace(metadata.Title))
                {
                    book.Title = metadata.Title;
                }
                if (!string.IsNullOrWhiteSpace(metadata.Publisher))
                {
                    book.Publisher = metadata.Publisher;
                }
                book.CoverImageUrl = coverImageUrl;
                book.PublishedDate = metadata.PublishedDate;
                
                // Update description chỉ nếu không có description từ trước
                if (string.IsNullOrEmpty(book.Description) && !string.IsNullOrWhiteSpace(metadata.Description))
                {
                    book.Description = metadata.Description;
                }

                // 9. Commit tất cả DB changes trong một transaction duy nhất
                await unitOfWork.ChapterRepository.AddRangeAsync(chapters);
                await unitOfWork.BookRepository.UpdateAsync(book);
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
    
    private async Task<EpubMetadata> ExtractMetadata(string filePath)
    {
        var epubBook = await EpubReader.ReadBookAsync(filePath);
        
        // Extract metadata from Schema.Package instead of direct properties
        var metadata = epubBook.Schema.Package.Metadata;
        
        // Extract publication date
        DateTime? publishedDate = null;
        var dateElements = metadata.Dates;
        if (dateElements?.Any() == true)
        {
            // Look for publication date first, then any date
            var publicationDate = dateElements.FirstOrDefault(d => 
                d.Event?.Equals("publication", StringComparison.OrdinalIgnoreCase) == true);
            
            var dateToUse = publicationDate ?? dateElements.First();
            
            if (DateTime.TryParse(dateToUse.Date, out var parsedDate))
            {
                publishedDate = parsedDate;
            }
        }
        
        return new EpubMetadata
        {
            Title = epubBook.Title ?? string.Empty,
            Author = string.Join(", ", epubBook.AuthorList ?? new List<string>()),
            Publisher = metadata.Publishers?.FirstOrDefault()?.Publisher ?? string.Empty,
            Language = metadata.Languages?.FirstOrDefault()?.Language ?? "vi",
            Description = metadata.Descriptions?.FirstOrDefault()?.Description ?? string.Empty,
            CoverImageBytes = epubBook.CoverImage,
            TotalPages = epubBook.ReadingOrder?.Count ?? 0,
            PublishedDate = publishedDate
        };
    }

    private async Task<string?> UploadCoverImage(byte[] imageBytes, Guid bookId, IStorageService storageService)
    {
        try
        {
            var originalFileName = $"epub-cover-{bookId}.jpg";
            var fileName = FileNameSanitizer.CreateUniqueFileName(originalFileName);
            using var memoryStream = new MemoryStream(imageBytes);
            
            var filePath = await storageService.UploadFileAsync(
                memoryStream, 
                fileName, 
                "image/jpeg", 
                "books/covers"
            );
            
            _logger.LogInformation("Uploaded cover image for book {BookId}: {FilePath}", bookId, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload cover image for book {BookId}", bookId);
            return null;
        }
    }
} 