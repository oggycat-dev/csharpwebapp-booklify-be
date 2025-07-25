using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.User.Queries.GetSubscriptionHistory;

public class GetSubscriptionHistoryQueryHandler : IRequestHandler<GetSubscriptionHistoryQuery, Result<List<UserSubscriptionHistoryResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetSubscriptionHistoryQueryHandler> _logger;

    public GetSubscriptionHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetSubscriptionHistoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<UserSubscriptionHistoryResponse>>> Handle(GetSubscriptionHistoryQuery request, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<List<UserSubscriptionHistoryResponse>>.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        try
        {
            // Get user profile first to ensure user exists
            var userProfile = await _unitOfWork.UserProfileRepository
                .GetFirstOrDefaultAsync(x => x.Id == request.UserId);

            if (userProfile == null)
            {
                return Result<List<UserSubscriptionHistoryResponse>>.Failure(
                    "User not found",
                    ErrorCode.NotFound);
            }

            // Get subscription history with subscription details
            var subscriptionHistory = await _unitOfWork.UserSubscriptionRepository
                .GetSubscriptionHistoryByUserIdAsync(request.UserId);

            // Map to response
            var response = _mapper.Map<List<UserSubscriptionHistoryResponse>>(subscriptionHistory);

            return Result<List<UserSubscriptionHistoryResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history for user ID: {UserId}", request.UserId);
            return Result<List<UserSubscriptionHistoryResponse>>.Failure(
                "Error getting subscription history",
                ErrorCode.InternalError);
        }
    }
} 