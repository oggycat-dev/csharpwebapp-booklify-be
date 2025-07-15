using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.Book.Queries.GetBookStatistics;

/// <summary>
/// Handler cho GetBookStatisticsQuery
/// </summary>
public class GetBookStatisticsQueryHandler : IRequestHandler<GetBookStatisticsQuery, Result<BookStatisticsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetBookStatisticsQueryHandler> _logger;

    public GetBookStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetBookStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<BookStatisticsResponse>> Handle(GetBookStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user authentication
            var isUserValid = await _currentUserService.IsUserValidAsync();
            if (!isUserValid)
            {
                return Result<BookStatisticsResponse>.Failure("User Unauthorized", ErrorCode.Unauthorized);
            }

            // Check if user has admin or staff role
            var userRoles = _currentUserService.Roles.ToList();
            bool isAdminOrStaff = userRoles.Contains("Admin") || userRoles.Contains("Staff");

            if (!isAdminOrStaff)
            {
                return Result<BookStatisticsResponse>.Failure("Insufficient permissions", ErrorCode.Forbidden);
            }

            // Get book statistics from repository
            var statistics = await _unitOfWork.BookRepository.GetBookStatisticsAsync();

            _logger.LogInformation("Successfully retrieved book statistics for user {UserId}", _currentUserService.UserId);

            return Result<BookStatisticsResponse>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting book statistics");
            return Result<BookStatisticsResponse>.Failure("Lỗi khi lấy thống kê sách", ErrorCode.InternalError);
        }
    }
} 