using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.BusinessLogic;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.Book.Queries.DownloadBook;

/// <summary>
/// Handler để xử lý download file sách
/// </summary>
public class DownloadBookQueryHandler : IRequestHandler<DownloadBookQuery, Result<BookDownloadResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBookBusinessLogic _businessLogic;
    private readonly ILogger<DownloadBookQueryHandler> _logger;

    public DownloadBookQueryHandler(
        IUnitOfWork unitOfWork,
        IFileService fileService,
        ICurrentUserService currentUserService,
        IBookBusinessLogic businessLogic,
        ILogger<DownloadBookQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _currentUserService = currentUserService;
        _businessLogic = businessLogic;
        _logger = logger;
    }

    public async Task<Result<BookDownloadResponse>> Handle(DownloadBookQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Lấy thông tin sách với file info
            var book = await _unitOfWork.BookRepository.GetByIdAsync(
                request.BookId,
                b => b.Category,
                b => b.File);

            if (book == null)
            {
                return Result<BookDownloadResponse>.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            // Kiểm tra quyền truy cập dựa trên business logic có sẵn
            var accessResult = await ValidateBookAccessAsync(book);
            if (!accessResult.IsSuccess)
            {
                return Result<BookDownloadResponse>.Failure(accessResult.Message, accessResult.ErrorCode ?? ErrorCode.InternalError);
            }

            // Kiểm tra file tồn tại
            if (string.IsNullOrEmpty(book.FilePath))
            {
                return Result<BookDownloadResponse>.Failure("Sách không có file để download", ErrorCode.NotFound);
            }

            // Download file từ storage
            var originalFileName = book.File?.Name ?? Path.GetFileName(book.FilePath);
            var downloadResult = await _fileService.GetFileAsync(book.FilePath, originalFileName);
            
            if (!downloadResult.IsSuccess)
            {
                _logger.LogError("Failed to download file for book {BookId}: {Error}", request.BookId, downloadResult.Message);
                return Result<BookDownloadResponse>.Failure("Lỗi khi tải file", ErrorCode.InternalError);
            }

            var (fileContent, contentType, fileName) = downloadResult.Data;

            var response = new BookDownloadResponse
            {
                FileContent = fileContent,
                ContentType = contentType,
                FileName = fileName
            };

            _logger.LogInformation("Book {BookId} downloaded successfully by user {UserId}", 
                request.BookId, _currentUserService.UserId);

            return Result<BookDownloadResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading book {BookId}", request.BookId);
            return Result<BookDownloadResponse>.Failure("Lỗi khi tải sách", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Validate book access using similar logic from GetBookByIdAsync in business logic
    /// </summary>
    private async Task<Result> ValidateBookAccessAsync(Domain.Entities.Book book)
    {
        // Determine user role and authentication status
        bool isAuthenticated = _currentUserService.IsAuthenticated;
        var userRoles = _currentUserService.Roles?.ToList() ?? new List<string>();
        bool isAdminOrStaff = userRoles.Contains("Admin") || userRoles.Contains("Staff");

        // Status validation based on role
        if (!isAdminOrStaff)
        {
            // Guest and User can only download Active + Approved books
            if (book.Status != Domain.Enums.EntityStatus.Active || 
                book.ApprovalStatus != Domain.Enums.ApprovalStatus.Approved)
            {
                return Result.Failure("Không tìm thấy sách hoặc sách không được phép tải", ErrorCode.NotFound);
            }

            // Additional check for premium books
            if (book.IsPremium)
            {
                bool hasFullAccess = false;

                if (isAuthenticated)
                {
                    // Check subscription for authenticated users
                    hasFullAccess = await CheckPremiumAccessAsync();
                }

                if (!hasFullAccess)
                {
                    return Result.Failure("Sách này yêu cầu subscription để tải xuống", ErrorCode.Forbidden);
                }
            }
        }
        // Admin/Staff can download all books regardless of status

        return Result.Success();
    }

    /// <summary>
    /// Check if user has premium access (reused logic from BookBusinessLogic)
    /// </summary>
    private async Task<bool> CheckPremiumAccessAsync()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return false;
        }

        var userRoles = _currentUserService.Roles.ToList();

        // Admin và Staff có quyền truy cập đầy đủ
        if (userRoles.Contains("Admin") || userRoles.Contains("Staff"))
        {
            return true;
        }

        // Kiểm tra subscription cho User role
        if (userRoles.Contains("User"))
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            // Lấy user profile và kiểm tra subscription
            var userProfile = await _unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(
                u => u.IdentityUserId == userId,
                u => u.UserSubscriptions);

            if (userProfile?.UserSubscriptions == null || !userProfile.UserSubscriptions.Any())
            {
                return false;
            }

            // Kiểm tra có subscription active không
            var activeSubscription = userProfile.UserSubscriptions
                .FirstOrDefault(s => s.Status == Domain.Enums.EntityStatus.Active &&
                               s.StartDate <= DateTime.UtcNow && 
                               s.EndDate >= DateTime.UtcNow);

            return activeSubscription != null;
        }

        return false;
    }
}