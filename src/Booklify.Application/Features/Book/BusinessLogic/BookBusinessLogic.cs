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
    /// Create book with complete EPUB processing workflow using IFormFile content
    /// </summary>
    public async Task<Result<Domain.Entities.Book>> CreateBookWithEpubProcessingAsync(
        object bookRequest,
        StaffProfile staff,
        Domain.Entities.FileInfo fileInfo,
        Microsoft.AspNetCore.Http.IFormFile originalFile,
        string userId,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEPubService epubService,
        IStorageService storageService,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        // Create book entity first
        var bookResult = await CreateBookEntityAsync(bookRequest, staff, fileInfo, userId, mapper, unitOfWork);
        if (!bookResult.IsSuccess)
        {
            return bookResult;
        }

        var book = bookResult.Data!;

        // Check if file is EPUB and extract metadata using IFormFile
        var shouldProcessEpub = ShouldProcessEpub(originalFile);
        if (shouldProcessEpub)
        {
            try
            {
                logger.LogInformation("Detected EPUB file for book {BookId}, extracting metadata from IFormFile", book.Id);
                
                // Extract metadata using IFormFile stream directly
                var metadataResult = await ExtractAndApplyEpubMetadataFromFormFileAsync(
                    book, originalFile, epubService, storageService, logger);
                
                if (metadataResult.IsSuccess)
                {
                    logger.LogInformation("Successfully applied EPUB metadata to book {BookId} from IFormFile", book.Id);
                }
                else
                {
                    logger.LogWarning("Failed to extract EPUB metadata from IFormFile for book {BookId}: {Message}", 
                        book.Id, metadataResult.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to extract EPUB metadata from IFormFile during book creation for book {BookId}", book.Id);
                // Continue with book creation even if metadata extraction fails
            }
        }

        return Result<Domain.Entities.Book>.Success(book);
    }

    /// <summary>
    /// Extract metadata from EPUB using IFormFile and apply to book entity
    /// </summary>
    private async Task<Result<bool>> ExtractAndApplyEpubMetadataFromFormFileAsync(
        Domain.Entities.Book book,
        Microsoft.AspNetCore.Http.IFormFile formFile,
        IEPubService epubService,
        IStorageService storageService,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            // Get temporary file path for metadata extraction
            var tempFilePath = Path.GetTempFileName();
            tempFilePath = Path.ChangeExtension(tempFilePath, ".epub");
            
            // Copy IFormFile content to temp file
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                await formFile.CopyToAsync(fileStream);
            }
            
            // Extract metadata from EPUB
            var epubMetadata = await epubService.ExtractMetadataAsync(tempFilePath);
            
            // Apply metadata to book
            if (epubMetadata != null)
            {
                ApplyEpubMetadataToBook(book, epubMetadata, logger);
                
                // Upload cover image if available
                if (epubMetadata.CoverImageBytes != null && epubMetadata.CoverImageBytes.Length > 0)
                {
                    var coverImageUrl = await UploadCoverImageAsync(
                        epubMetadata.CoverImageBytes, storageService, logger);
                    
                    if (!string.IsNullOrEmpty(coverImageUrl))
                    {
                        book.CoverImageUrl = coverImageUrl;
                    }
                }
            }
            
            // Clean up temporary file
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
                
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during EPUB metadata extraction from IFormFile for book {BookId}", book.Id);
            return Result<bool>.Failure($"EPUB metadata extraction failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply extracted EPUB metadata to book entity
    /// </summary>
    private void ApplyEpubMetadataToBook(
        Domain.Entities.Book book, 
        EpubMetadataDto epubMetadata, 
        Microsoft.Extensions.Logging.ILogger logger)
    {
        // Set basic metadata
        if (!string.IsNullOrWhiteSpace(epubMetadata.Title))
        {
            book.Title = epubMetadata.Title;
            logger.LogDebug("Applied title: {Title}", epubMetadata.Title);
        }
        
        if (!string.IsNullOrWhiteSpace(epubMetadata.Author))
        {
            book.Author = epubMetadata.Author;
            logger.LogDebug("Applied author: {Author}", epubMetadata.Author);
        }
        
        if (!string.IsNullOrWhiteSpace(epubMetadata.Publisher))
        {
            book.Publisher = epubMetadata.Publisher;
            logger.LogDebug("Applied publisher: {Publisher}", epubMetadata.Publisher);
        }
        
        if (!string.IsNullOrWhiteSpace(epubMetadata.Description))
        {
            book.Description = epubMetadata.Description;
            logger.LogDebug("Applied description length: {Length}", epubMetadata.Description.Length);
        }
        
        // Note: ISBN is not extracted from EPUB metadata, it comes from user input only
        
        // Set other metadata
        if (epubMetadata.PublishedDate.HasValue)
        {
            book.PublishedDate = epubMetadata.PublishedDate;
            logger.LogDebug("Applied published date: {PublishedDate}", epubMetadata.PublishedDate);
        }
        
        if (epubMetadata.TotalPages > 0)
        {
            book.PageCount = epubMetadata.TotalPages;
            logger.LogDebug("Applied page count: {PageCount}", epubMetadata.TotalPages);
        }
        
        logger.LogInformation("Successfully applied EPUB metadata: Title={Title}, Author={Author}, Publisher={Publisher}", 
            epubMetadata.Title, epubMetadata.Author, epubMetadata.Publisher);
    }

    /// <summary>
    /// Upload cover image to storage
    /// </summary>
    private async Task<string?> UploadCoverImageAsync(
        byte[] coverImageBytes, 
        IStorageService storageService, 
        Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            var originalFileName = $"epub-cover-{Guid.NewGuid()}.jpg";
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{originalFileName}";
            using var memoryStream = new MemoryStream(coverImageBytes);
            
            var coverImageUrl = await storageService.UploadFileAsync(
                memoryStream, 
                fileName, 
                "image/jpeg", 
                "books/covers"
            );
            
            logger.LogInformation("Uploaded cover image for EPUB book: {CoverImageUrl}", coverImageUrl);
            return coverImageUrl;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload cover image");
            return null;
        }
    }

    /// <summary>
    /// Update book with complete EPUB processing workflow using IFormFile content
    /// </summary>
    public async Task<Result<Domain.Entities.Book>> UpdateBookWithEpubProcessingAsync(
        Domain.Entities.Book existingBook,
        Domain.Entities.FileInfo newFileInfo,
        Microsoft.AspNetCore.Http.IFormFile originalFile,
        string userId,
        IUnitOfWork unitOfWork,
        IEPubService epubService,
        IStorageService storageService,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        // Update file information
        existingBook.FilePath = newFileInfo.FilePath;
        existingBook.File = newFileInfo;

        // Check if file is EPUB and extract metadata using IFormFile
        var shouldProcessEpub = ShouldProcessEpub(originalFile);
        if (shouldProcessEpub)
        {
            try
            {
                logger.LogInformation("Detected EPUB file for book {BookId}, extracting metadata from IFormFile for update", existingBook.Id);
                
                // Extract metadata using IFormFile stream directly
                var metadataResult = await ExtractAndApplyEpubMetadataFromFormFileAsync(
                    existingBook, originalFile, epubService, storageService, logger);
                
                if (metadataResult.IsSuccess)
                {
                    logger.LogInformation("Successfully applied EPUB metadata to existing book {BookId} from IFormFile", existingBook.Id);
                }
                else
                {
                    logger.LogWarning("Failed to extract EPUB metadata from IFormFile for book update {BookId}: {Message}", 
                        existingBook.Id, metadataResult.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to extract EPUB metadata from IFormFile during book update for book {BookId}", existingBook.Id);
                // Continue with book update even if metadata extraction fails
            }
        }

        return Result<Domain.Entities.Book>.Success(existingBook);
    }

    /// <summary>
    /// Check if file is EPUB and should be processed
    /// </summary>
    public bool ShouldProcessEpub(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return false;
        }
        
        // Trim whitespace and convert to lowercase
        var cleanExtension = fileExtension.Trim().ToLowerInvariant();
        
        // Remove leading dot if present
        if (cleanExtension.StartsWith("."))
        {
            cleanExtension = cleanExtension.Substring(1);
        }
        
        var isEpub = cleanExtension == "epub";
        
        return isEpub;
    }

    /// <summary>
    /// Check if IFormFile is EPUB and should be processed
    /// </summary>
    public bool ShouldProcessEpub(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null)
        {
            return false;
        }

        // Check by file extension from filename
        var extension = Path.GetExtension(file.FileName);
        if (ShouldProcessEpub(extension))
        {
            return true;
        }

        // Fallback: Check by content type
        return file.ContentType?.ToLowerInvariant() == "application/epub+zip";
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
            
            // ShouldProcessEpub will be set later when we know the new file type
            jobData.ShouldProcessEpub = false;
            
            // FileContent and FileExtension will be set by caller if EPUB
            jobData.FileContent = null;
            jobData.FileExtension = null;
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
                logger.LogInformation("Detected new EPUB file for book {BookId}, queueing background processing for chapter extraction", bookId);
                
                if (jobData.FileContent != null && !string.IsNullOrEmpty(jobData.FileExtension))
                {
                    // Use file content directly if available (avoids re-downloading)
                    logger.LogInformation("Using provided file content for EPUB processing for book {BookId}", bookId);
                    var epubJobId = epubService.ProcessEpubFileWithContent(bookId, userId, jobData.FileContent, jobData.FileExtension);
                    logger.LogInformation("EPUB processing job queued with ID: {JobId} - this will extract chapters", epubJobId);
                }
                else
                {
                    logger.LogWarning("No file content provided for EPUB processing for book {BookId}", bookId);
                }
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
    /// Queue EPUB background processing job with file content from IFormFile
    /// </summary>
    public void QueueEpubProcessingWithFileContent(
        Guid bookId,
        string userId,
        byte[] fileContent,
        string fileExtension,
        IEPubService epubService,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            logger.LogInformation("Queueing EPUB processing job for book {BookId} with provided file content", bookId);
            var jobId = epubService.ProcessEpubFileWithContent(bookId, userId, fileContent, fileExtension);
            logger.LogInformation("EPUB processing job queued with ID: {JobId} for book {BookId}", jobId, bookId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to queue EPUB processing with file content for book: {BookId}", bookId);
            // Background job failure doesn't affect main operation - book was created successfully
        }
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
    /// Get book detail by ID without chapters (lighter response)
    /// </summary>
    public async Task<Result<BookDetailResponse>> GetBookDetailByIdAsync(
        Guid bookId,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        ICurrentUserService? currentUserService = null)
    {
        try
        {
            // Get book without chapters for lighter response
            var book = await unitOfWork.BookRepository.GetByIdAsync(
                bookId,
                b => b.Category,
                b => b.File);

            if (book == null)
            {
                return Result<BookDetailResponse>.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            // Determine user role and authentication status
            bool isAuthenticated = currentUserService?.IsAuthenticated ?? false;
            var userRoles = currentUserService?.Roles?.ToList() ?? new List<string>();
            bool isAdminOrStaff = userRoles.Contains(Role.Admin.ToString()) || userRoles.Contains(Role.Staff.ToString());

            // Status validation based on role
            if (!isAdminOrStaff)
            {
                // Guest and User can only see Active + Approved books
                if (book.Status != EntityStatus.Active || book.ApprovalStatus != ApprovalStatus.Approved)
                {
                    return Result<BookDetailResponse>.Failure("Không tìm thấy sách hoặc sách không được phép xem", ErrorCode.NotFound);
                }
            }
            // Admin/Staff can see all books regardless of status

            // Map to response DTO
            var response = mapper.Map<BookDetailResponse>(book);

            // Enrich with file URLs
            if (!string.IsNullOrEmpty(book.FilePath))
            {
                response.FileUrl = fileService.GetFileUrl(book.FilePath);
            }
            if (!string.IsNullOrEmpty(book.CoverImageUrl))
            {
                response.CoverImageUrl = fileService.GetFileUrl(book.CoverImageUrl);
            }

            // Check if book has chapters (without loading them)
            response.HasChapters = await unitOfWork.ChapterRepository.AnyAsync(c => c.BookId == bookId);

            // ✅ Get resume reading information for authenticated users
            await EnrichWithReadingProgressAsync(response, bookId, currentUserService, unitOfWork);

            return Result<BookDetailResponse>.Success(response);
        }
        catch (Exception)
        {
            return Result<BookDetailResponse>.Failure("Lỗi khi lấy thông tin sách", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Enrich BookDetailResponse with reading progress information for User role only
    /// </summary>
    private async Task EnrichWithReadingProgressAsync(
        BookDetailResponse response, 
        Guid bookId, 
        ICurrentUserService? currentUserService, 
        IUnitOfWork unitOfWork)
    {
        // ✅ Initialize all reading progress fields as null (will be ignored in JSON for Admin/Staff)
        response.HasReadingProgress = null;
        response.CurrentChapterId = null;
        response.CurrentCfi = null;
        response.ReadingProgressPercentage = null;
        response.CompletedChaptersCount = null;
        response.LastReadAt = null;
        response.IsBookCompleted = null;

        // ✅ Only get reading progress for authenticated users with "User" role
        if (currentUserService?.IsAuthenticated != true || string.IsNullOrEmpty(currentUserService.UserId))
        {
            return;
        }

        var userRoles = currentUserService.Roles?.ToList() ?? new List<string>();
        bool isUser = userRoles.Contains(Role.User.ToString());
        bool isAdminOrStaff = userRoles.Contains(Role.Admin.ToString()) || userRoles.Contains(Role.Staff.ToString());

        // ✅ Admin and Staff don't use tracking functionality - leave fields as null
        if (isAdminOrStaff)
        {
            return;
        }

        // ✅ Only process reading progress for User role
        if (!isUser)
        {
            return;
        }

        try
        {
            // Get user profile
            var userProfile = await unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(
                x => x.IdentityUserId == currentUserService.UserId);

            if (userProfile == null)
            {
                return;
            }

            // Get reading progress for this book
            var readingProgress = await unitOfWork.ReadingProgressRepository.GetFirstOrDefaultAsync(
                x => x.UserId == userProfile.Id && x.BookId == bookId);

            if (readingProgress == null)
            {
                // ✅ No reading progress found for User - set explicit values (not null, so they appear in JSON)
                response.HasReadingProgress = false;
                response.ReadingProgressPercentage = 0;
                response.CompletedChaptersCount = 0;
                response.IsBookCompleted = false;
                // CurrentChapterId, CurrentCfi, LastReadAt remain null
                return;
            }

            // ✅ Found reading progress - populate resume reading info for User role
            response.HasReadingProgress = true;
            response.CurrentChapterId = readingProgress.CurrentChapterId;
            response.CompletedChaptersCount = readingProgress.CompletedChaptersCount;
            response.LastReadAt = readingProgress.LastReadAt;
            response.IsBookCompleted = readingProgress.IsCompleted;

            // Calculate reading progress percentage
            if (response.TotalChapters > 0)
            {
                response.ReadingProgressPercentage = Math.Round(
                    (double)readingProgress.CompletedChaptersCount / response.TotalChapters * 100, 2);
            }
            else
            {
                response.ReadingProgressPercentage = 0;
            }

            // Get current CFI from the latest chapter reading progress
            if (readingProgress.CurrentChapterId.HasValue)
            {
                var currentChapterProgress = await unitOfWork.ChapterReadingProgressRepository.GetFirstOrDefaultAsync(
                    x => x.ReadingProgressId == readingProgress.Id && x.ChapterId == readingProgress.CurrentChapterId.Value);

                if (currentChapterProgress != null)
                {
                    response.CurrentCfi = currentChapterProgress.CurrentCfi;
                }
            }
        }
        catch (Exception)
        {
            // If any error occurs getting reading progress, just leave default values
            // Don't let reading progress errors break the main book detail response
        }
    }

    /// <summary>
    /// Check if user has premium access (Admin, Staff, or active subscription)
    /// </summary>
    private async Task<bool> CheckPremiumAccessAsync(ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
    {
        // Guest user - không có quyền
        if (!currentUserService.IsAuthenticated)
        {
            return false;
        }

        var userRoles = currentUserService.Roles.ToList();

        // Admin và Staff có quyền truy cập đầy đủ
        if (userRoles.Contains("Admin") || userRoles.Contains("Staff"))
        {
            return true;
        }

        // Kiểm tra subscription cho User role
        if (userRoles.Contains("User"))
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            // Lấy user profile và kiểm tra subscription
            var userProfile = await unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(
                u => u.IdentityUserId == userId,
                u => u.UserSubscriptions);

            if (userProfile?.UserSubscriptions == null || !userProfile.UserSubscriptions.Any())
            {
                return false;
            }

            // Kiểm tra có subscription active không
            var activeSubscription = userProfile.UserSubscriptions
                .FirstOrDefault(s => s.IsActive && 
                                   s.Status == Domain.Enums.EntityStatus.Active &&
                                   s.StartDate <= DateTime.UtcNow && 
                                   s.EndDate >= DateTime.UtcNow);

            return activeSubscription != null;
        }

        return false;
    }

    /// <summary>
    /// Map chapters to response DTOs manually (không sử dụng AutoMapper)
    /// </summary>
    private List<ChapterResponse> MapChaptersToResponse(ICollection<Domain.Entities.Chapter> chapters)
    {
        var chapterList = chapters.OrderBy(c => c.Order).ToList();
        var chapterDict = new Dictionary<Guid, ChapterResponse>();
        var rootChapters = new List<ChapterResponse>();
        
        // First pass: Create all chapter responses
        foreach (var chapter in chapterList)
        {
            var title = CleanupChapterTitle(chapter.Title);
            
            var chapterResponse = new ChapterResponse
            {
                Id = chapter.Id,
                Title = title,
                Order = chapter.Order,
                Href = chapter.Href,
                Cfi = chapter.Cfi,
                ParentChapterId = chapter.ParentChapterId,
                ChildChapters = new List<ChapterResponse>()
            };
            
            chapterDict[chapter.Id] = chapterResponse;
        }
        
        // Second pass: Build hierarchy based on ParentChapterId
        foreach (var chapter in chapterList)
        {
            var chapterResponse = chapterDict[chapter.Id];
            
            // Skip system files
            if (IsSystemFile(chapter.Title))
            {
                continue;
            }
            
            if (chapter.ParentChapterId == null)
            {
                // Root level chapter (likely a Part)
                rootChapters.Add(chapterResponse);
            }
            else if (chapterDict.ContainsKey(chapter.ParentChapterId.Value))
            {
                // Add to parent's children
                var parentResponse = chapterDict[chapter.ParentChapterId.Value];
                parentResponse.ChildChapters?.Add(chapterResponse);
            }
            else
            {
                // Parent not found (shouldn't happen), add to root
                rootChapters.Add(chapterResponse);
            }
        }
        
        // Sort all levels by Order
        rootChapters = rootChapters.OrderBy(c => c.Order).ToList();
        foreach (var chapter in rootChapters)
        {
            if (chapter.ChildChapters != null)
            {
                chapter.ChildChapters = chapter.ChildChapters.OrderBy(c => c.Order).ToList();
            }
        }
        
        return rootChapters;
    }
    
    private bool IsSystemFile(string title)
    {
        return title.StartsWith("cover") ||
               title.StartsWith("index") ||
               title.StartsWith("toc") ||
               title.StartsWith("bk01-toc") ||
               title.StartsWith("pr01");
    }
    
    private string CleanupChapterTitle(string rawTitle)
    {
        // Extract number from pt01 or ch01 format
        var match = System.Text.RegularExpressions.Regex.Match(rawTitle, @"^(pt|ch)(\d+)$");
        if (match.Success)
        {
            var prefix = match.Groups[1].Value;
            var number = int.Parse(match.Groups[2].Value);
            
            return prefix == "pt" 
                ? $"Part {number}" 
                : $"Chapter {number}";
        }
        
        // Handle subsections like ch14s01
        match = System.Text.RegularExpressions.Regex.Match(rawTitle, @"^ch(\d+)s(\d+)$");
        if (match.Success)
        {
            var chapter = int.Parse(match.Groups[1].Value);
            var section = int.Parse(match.Groups[2].Value);
            return $"Chapter {chapter} Section {section}";
        }
        
        return rawTitle; // Fallback to original if no pattern matches
    }

    /// <summary>
    /// Get paged books with basic information for client view
    /// </summary>
    public async Task<Result<PaginatedResult<BookListItemResponse>>> GetPagedBookListItemsAsync(
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
            
            // Map to list item response DTOs using AutoMapper
            var bookResponses = mapper.Map<List<BookListItemResponse>>(books);
            
            // Enrich cover image URLs
            foreach (var response in bookResponses)
            {
                var book = books.First(b => b.Id == response.Id);
                if (!string.IsNullOrEmpty(book.CoverImageUrl))
                {
                    response.CoverImageUrl = fileService.GetFileUrl(book.CoverImageUrl);
                }
            }
            
            // Return paginated result
            var result = PaginatedResult<BookListItemResponse>.Success(
                bookResponses, 
                filterToUse.PageNumber,
                filterToUse.PageSize,
                totalCount);
                
            return Result<PaginatedResult<BookListItemResponse>>.Success(result);
        }
        catch (Exception)
        {
            return Result<PaginatedResult<BookListItemResponse>>.Failure(
                "Lỗi khi lấy danh sách sách", 
                ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Read file content from IFormFile as byte array
    /// </summary>
    private static async Task<byte[]> ReadFileContentAsync(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Get book chapters by book ID with role-based access control and subscription checks
    /// </summary>
    public async Task<Result<List<ChapterResponse>>> GetBookChaptersAsync(
        Guid bookId,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService? currentUserService = null)
    {
        try
        {
            // Check if book exists and get basic info
            var book = await unitOfWork.BookRepository.GetByIdAsync(
                bookId,
                b => b.Chapters);

            if (book == null)
            {
                return Result<List<ChapterResponse>>.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            // Determine user role and authentication status
            bool isAuthenticated = currentUserService?.IsAuthenticated ?? false;
            var userRoles = currentUserService?.Roles?.ToList() ?? new List<string>();
            bool isAdminOrStaff = userRoles.Contains("Admin") || userRoles.Contains("Staff");
            bool isUser = userRoles.Contains("User");
            bool isGuest = !isAuthenticated;

            // Status validation based on role (same as book detail)
            if (!isAdminOrStaff)
            {
                // Guest and User can only access chapters of Active + Approved books
                if (book.Status != EntityStatus.Active || book.ApprovalStatus != ApprovalStatus.Approved)
                {
                    return Result<List<ChapterResponse>>.Failure("Không tìm thấy sách hoặc sách không được phép xem", ErrorCode.NotFound);
                }
            }
            // Admin/Staff can access chapters of all books regardless of status

            // Handle chapters based on role and premium status
            var chaptersToReturn = new List<Domain.Entities.Chapter>();

            if (book.Chapters != null && book.Chapters.Any())
            {
                if (book.IsPremium && !isAdminOrStaff)
                {
                    // Premium book for non-admin/staff users
                    bool hasFullAccess = false;

                    if (isUser && currentUserService != null)
                    {
                        // Check subscription for authenticated users
                        hasFullAccess = await CheckPremiumAccessAsync(currentUserService, unitOfWork);
                    }
                    // Guest users (isGuest = true) will have hasFullAccess = false

                    if (hasFullAccess)
                    {
                        // User with active subscription - return all chapters
                        chaptersToReturn = book.Chapters.ToList();
                    }
                    else
                    {
                        // Guest or User without subscription - limit to first 2 chapters
                        chaptersToReturn = book.Chapters
                            .OrderBy(c => c.Order)
                            .Take(2)
                            .ToList();
                    }
                }
                else
                {
                    // Non-premium book OR Admin/Staff - return all chapters
                    chaptersToReturn = book.Chapters.ToList();
                }
            }

            // Map to response DTOs with hierarchy
            var chapterResponses = MapChaptersToResponse(chaptersToReturn);

            return Result<List<ChapterResponse>>.Success(chapterResponses);
        }
        catch (Exception)
        {
            return Result<List<ChapterResponse>>.Failure("Lỗi khi lấy danh sách chapters", ErrorCode.InternalError);
        }
    }
}