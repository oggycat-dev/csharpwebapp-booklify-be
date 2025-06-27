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
    /// Get book by ID with enrichment
    /// </summary>
    Task<Result<BookResponse>> GetBookByIdAsync(
        Guid bookId,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService);
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