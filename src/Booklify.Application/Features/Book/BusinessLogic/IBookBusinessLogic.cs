using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;

namespace Booklify.Application.Features.Book.BusinessLogic;

/// <summary>
/// Interface for book business logic operations
/// </summary>
public interface IBookBusinessLogic
{
    /// <summary>
    /// Validate user authentication and get staff information
    /// </summary>
    Task<Result<StaffProfile>> ValidateUserAndGetStaffAsync(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Validate book category exists
    /// </summary>
    Task<Result<bool>> ValidateBookCategoryAsync(
        Guid categoryId,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Enrich book response with full URLs
    /// </summary>
    BookResponse EnrichBookResponse(
        BookResponse response,
        Domain.Entities.Book book,
        IFileService fileService);

    /// <summary>
    /// Create and upload file for book
    /// </summary>
    Task<Result<Domain.Entities.FileInfo>> CreateBookFileAsync(
        Microsoft.AspNetCore.Http.IFormFile file,
        string subDirectory,
        string userId,
        IFileService fileService,
        IStorageService storageService,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Create book entity with business rules
    /// </summary>
    Task<Result<Domain.Entities.Book>> CreateBookEntityAsync(
        object bookRequest,
        StaffProfile staff,
        Domain.Entities.FileInfo fileInfo,
        string userId,
        IMapper mapper,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Create book with complete EPUB processing workflow
    /// </summary>
    Task<Result<Domain.Entities.Book>> CreateBookWithEpubProcessingAsync(
        object bookRequest,
        StaffProfile staff,
        Domain.Entities.FileInfo fileInfo,
        string userId,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEPubService epubService,
        IStorageService storageService,
        Microsoft.Extensions.Logging.ILogger logger);

    /// <summary>
    /// Update book with complete EPUB processing workflow (for new file uploads)
    /// </summary>
    Task<Result<Domain.Entities.Book>> UpdateBookWithEpubProcessingAsync(
        Domain.Entities.Book existingBook,
        Domain.Entities.FileInfo newFileInfo,
        string userId,
        IUnitOfWork unitOfWork,
        IEPubService epubService,
        IStorageService storageService,
        Microsoft.Extensions.Logging.ILogger logger);

    /// <summary>
    /// Check if file is EPUB and should be processed
    /// </summary>
    bool ShouldProcessEpub(string fileExtension);

    /// <summary>
    /// Prepare background job data before transaction for book updates
    /// </summary>
    Task<Result<BookUpdateJobData>> PrepareBookUpdateJobDataAsync(
        Domain.Entities.Book book,
        bool hasNewFile,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Queue background jobs after successful transaction commit
    /// </summary>
    void QueueBookBackgroundJobs(
        BookUpdateJobData jobData,
        Guid bookId,
        string userId,
        IFileBackgroundService fileBackgroundService,
        IEPubService epubService,
        Microsoft.Extensions.Logging.ILogger logger);

    /// <summary>
    /// Get paged books with filters and enrichment
    /// </summary>
    Task<Result<PaginatedResult<BookResponse>>> GetPagedBooksAsync(
        BookFilterModel filter,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService);

    

    /// <summary>
    /// Get book detail by ID without chapters (lighter response)
    /// </summary>
    Task<Result<BookDetailResponse>> GetBookDetailByIdAsync(
        Guid bookId,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        ICurrentUserService? currentUserService = null);

    /// <summary>
    /// Get paged books with basic information for client view
    /// </summary>
    Task<Result<PaginatedResult<BookListItemResponse>>> GetPagedBookListItemsAsync(
        BookFilterModel filter,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService);

    /// <summary>
    /// Get book chapters by book ID with role-based access control and subscription checks
    /// </summary>
    Task<Result<List<ChapterResponse>>> GetBookChaptersAsync(
        Guid bookId,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService? currentUserService = null);
}

/// <summary>
/// Data transfer object for background job operations
/// </summary>
public class BookUpdateJobData
{
    public bool HasChaptersToDelete { get; set; }
    public string? CoverImageToDelete { get; set; }
    public string? FilePathToDelete { get; set; }
    public Guid? FileIdToDelete { get; set; }
    public bool ShouldProcessEpub { get; set; }
}