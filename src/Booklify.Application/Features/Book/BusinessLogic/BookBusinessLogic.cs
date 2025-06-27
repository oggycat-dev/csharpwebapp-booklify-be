using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;


namespace Booklify.Application.Features.Book.BusinessLogic;

/// <summary>
/// Business logic implementation for book operations
/// Follows clean architecture principles with method parameters instead of injected dependencies
/// </summary>
public class BookBusinessLogic : IBookBusinessLogic
{
    /// <summary>
    /// Validate user authentication and get staff information
    /// </summary>
    public async Task<Result<StaffProfile>> ValidateUserAndGetStaffAsync(
        ICurrentUserService currentUserService, 
        IUnitOfWork unitOfWork)
    {
        var isUserValid = await currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<StaffProfile>.Failure("User Unauthorized", ErrorCode.Unauthorized);
        }

        var currentUserId = currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Result<StaffProfile>.Failure("User Unauthorized", ErrorCode.Unauthorized);
        }

        var staff = await unitOfWork.StaffProfileRepository.GetFirstOrDefaultAsync(
            s => s.IdentityUserId == currentUserId,
            s => s.IdentityUser);

        if (staff == null)
        {
            return Result<StaffProfile>.Failure("Staff Not Found", ErrorCode.NotFound);
        }

        return Result<StaffProfile>.Success(staff);
    }

    /// <summary>
    /// Validate book category exists
    /// </summary>
    public async Task<Result<bool>> ValidateBookCategoryAsync(
        Guid categoryId,
        IUnitOfWork unitOfWork)
    {
        var categoryExists = await unitOfWork.BookCategoryRepository.AnyAsync(
            c => c.Id == categoryId && c.Status == EntityStatus.Active);

        if (!categoryExists)
        {
            return Result<bool>.Failure("Danh mục sách không tồn tại hoặc không hoạt động", ErrorCode.NotFound);
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Enrich book response with full URLs
    /// </summary>
    public BookResponse EnrichBookResponse(
        BookResponse response,
        Domain.Entities.Book book,
        IFileService fileService)
    {
        // Set file URL
        if (!string.IsNullOrEmpty(book.FilePath))
        {
            response.FileUrl = fileService.GetFileUrl(book.FilePath);
        }

        // Set cover image URL
        if (!string.IsNullOrEmpty(book.CoverImageUrl))
        {
            response.CoverImageUrl = fileService.GetFileUrl(book.CoverImageUrl);
        }

        return response;
    }

    /// <summary>
    /// Create and upload file for book using file service
    /// </summary>
    public async Task<Result<Domain.Entities.FileInfo>> CreateBookFileAsync(
        IFormFile file,
        string subDirectory,
        string userId,
        IFileService fileService, 
        IStorageService storageService,
        IUnitOfWork unitOfWork)
    {
        try
        {
            // Use file service to upload file (includes file name sanitization)
            var uploadResult = await fileService.UploadFileAsync(file, subDirectory, userId);
            
            if (!uploadResult.IsSuccess)
            {
                return Result<Domain.Entities.FileInfo>.Failure($"Lỗi khi upload tệp tin: {uploadResult.Message}", ErrorCode.FileUploadFailed);
            }

            var uploadData = uploadResult.Data;

            // Create FileInfo entity
            var fileInfo = new Domain.Entities.FileInfo
            {
                FilePath = uploadData.FilePath,
                Name = uploadData.OriginalFileName,
                MimeType = uploadData.MimeType,
                Extension = uploadData.Extension,
                SizeKb = (int)uploadData.SizeKb,
                ServerUpload = storageService.GetType().Name,
                Provider = storageService.GetType().Name
            };

            BaseEntityExtensions.InitializeBaseEntity(fileInfo, userId);
            await unitOfWork.FileInfoRepository.AddAsync(fileInfo);
            
            return Result<Domain.Entities.FileInfo>.Success(fileInfo);
        }
        catch (Exception ex)
        {
            return Result<Domain.Entities.FileInfo>.Failure($"Lỗi khi upload tệp tin: {ex.Message}", ErrorCode.FileUploadFailed);
        }
    }

    /// <summary>
    /// Create book entity with business rules
    /// </summary>
    public async Task<Result<Domain.Entities.Book>> CreateBookEntityAsync(
        object bookRequest,
        StaffProfile staff,
        Domain.Entities.FileInfo fileInfo,
        string userId,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        var book = mapper.Map<Domain.Entities.Book>(bookRequest);
        book.FilePath = fileInfo.FilePath;
        book.File = fileInfo;
        book.ApprovalStatus = staff.Position == StaffPosition.Administrator ? ApprovalStatus.Approved : ApprovalStatus.Pending;

        BaseEntityExtensions.InitializeBaseEntity(book, userId);
        await unitOfWork.BookRepository.AddAsync(book);
        
        return Result<Domain.Entities.Book>.Success(book);
    }

    /// <summary>
    /// Check if file is EPUB and should be processed
    /// </summary>
    public bool ShouldProcessEpub(string fileExtension)
    {
        return fileExtension?.ToLowerInvariant() is ".epub" or "epub";
    }

    /// <summary>
    /// Prepare background job data before transaction for book updates
    /// This method is only called when there's a new file to process
    /// </summary>
    public async Task<Result<BookUpdateJobData>> PrepareBookUpdateJobDataAsync(
        Domain.Entities.Book book,
        bool hasNewFile,
        IUnitOfWork unitOfWork)
    {
        var jobData = new BookUpdateJobData();

        // This method should only be called when hasNewFile is true
        // but we'll keep the check for safety
        if (hasNewFile)
        {
            // Check for chapters to delete (only when replacing file)
            jobData.HasChaptersToDelete = await unitOfWork.ChapterRepository.AnyAsync(c => c.BookId == book.Id);
            
            // Store old file info for deletion (only when replacing file)
            jobData.CoverImageToDelete = book.CoverImageUrl;
            jobData.FilePathToDelete = book.FilePath;
            jobData.FileIdToDelete = book.File?.Id;
            
            // ShouldProcessEpub will be set later when we know the new file extension
            jobData.ShouldProcessEpub = false;
        }

        return Result<BookUpdateJobData>.Success(jobData);
    }

    /// <summary>
    /// Queue background jobs after successful transaction commit
    /// This method is only called when there's a new file being uploaded
    /// </summary>
    public void QueueBookBackgroundJobs(
        BookUpdateJobData jobData,
        Guid bookId,
        string userId,
        IFileBackgroundService fileBackgroundService,
        IEPubService epubService,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        logger.LogInformation("Starting background job queueing for book {BookId} due to file update", bookId);

        // Chapter deletion - only when there's a new file replacing the old one
        if (jobData.HasChaptersToDelete)
        {
            try
            {
                logger.LogInformation("Queueing chapter deletion for book {BookId} due to file replacement", bookId);
                var chapterJobId = fileBackgroundService.QueueChapterDeletionByBookId(bookId, userId);
                logger.LogInformation("Chapter deletion job queued with ID: {JobId}", chapterJobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue chapter deletion for book: {BookId}", bookId);
                // Background job failure không affect main operation
            }
        }

        // Old file deletion - only when there's a new file replacing the old one
        if (!string.IsNullOrEmpty(jobData.FilePathToDelete) && jobData.FileIdToDelete.HasValue)
        {
            try
            {
                logger.LogInformation("Queueing old file deletion: {FilePath} due to file replacement", jobData.FilePathToDelete);
                var fileJobId = fileBackgroundService.QueueFileDelete(jobData.FilePathToDelete, userId, jobData.FileIdToDelete);
                logger.LogInformation("File deletion job queued with ID: {JobId}", fileJobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue file deletion for: {FilePath}", jobData.FilePathToDelete);
                // Background job failure không affect main operation
            }
        }

        // Cover image deletion - only when there's a new file that might have a new cover
        if (!string.IsNullOrEmpty(jobData.CoverImageToDelete))
        {
            try
            {
                logger.LogInformation("Queueing cover image deletion: {CoverImageUrl} due to file replacement", jobData.CoverImageToDelete);
                var coverJobId = fileBackgroundService.QueueFileDelete(jobData.CoverImageToDelete, userId, null);
                logger.LogInformation("Cover image deletion job queued with ID: {JobId}", coverJobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue cover image deletion for: {CoverImageUrl}", jobData.CoverImageToDelete);
                // Background job failure không affect main operation
            }
        }

        // EPUB processing - only when the new file is an EPUB
        if (jobData.ShouldProcessEpub && epubService != null)
        {
            try
            {
                logger.LogInformation("Detected new EPUB file for book {BookId}, queueing background processing for chapter extraction and cover image extraction", bookId);
                var epubJobId = epubService.ProcessEpubFile(bookId, userId);
                logger.LogInformation("EPUB processing job queued with ID: {JobId} - this will extract chapters and cover image", epubJobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue EPUB processing for book: {BookId}", bookId);
                // Background job failure không affect main operation
            }
        }

        logger.LogInformation("Completed background job queueing for book {BookId}", bookId);
    }

    /// <summary>
    /// Get paged books with filters and enrichment
    /// </summary>
    public async Task<Result<PaginatedResult<BookResponse>>> GetPagedBooksAsync(
        BookFilterModel filter,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService)
    {
        try
        {
            var filterToUse = filter ?? new BookFilterModel();
            
            // Get paged books from repository
            var (books, totalCount) = await unitOfWork.BookRepository.GetPagedBooksAsync(filterToUse);
            
            // Map to response DTOs using AutoMapper
            var bookResponses = mapper.Map<List<BookResponse>>(books);
            
            // Enrich with file URLs
            for (int i = 0; i < bookResponses.Count; i++)
            {
                bookResponses[i] = EnrichBookResponse(bookResponses[i], books[i], fileService);
            }
            
            // Return paginated result
            var result = PaginatedResult<BookResponse>.Success(
                bookResponses, 
                filterToUse.PageNumber,
                filterToUse.PageSize,
                totalCount);
                
            return Result<PaginatedResult<BookResponse>>.Success(result);
        }
        catch (Exception)
        {
            return Result<PaginatedResult<BookResponse>>.Failure("Lỗi khi lấy danh sách sách", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Get book by ID with enrichment
    /// </summary>
    public async Task<Result<BookResponse>> GetBookByIdAsync(
        Guid bookId,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService)
    {
        try
        {
            // Get book with related entities
            var book = await unitOfWork.BookRepository.GetByIdAsync(
                bookId,
                b => b.Category,
                b => b.File,
                b => b.Chapters);

            if (book == null)
            {
                return Result<BookResponse>.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            // Map to response DTO
            var response = mapper.Map<BookResponse>(book);

            // Enrich with file URLs and additional info
            response = EnrichBookResponse(response, book, fileService);

            return Result<BookResponse>.Success(response);
        }
        catch (Exception)
        {
            return Result<BookResponse>.Failure("Lỗi khi lấy thông tin sách", ErrorCode.InternalError);
        }
    }
} 