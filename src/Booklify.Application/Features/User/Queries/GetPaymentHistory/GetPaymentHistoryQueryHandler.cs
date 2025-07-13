using AutoMapper;
using Booklify.Application.Common.DTOs.Payment;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.User.Queries.GetPaymentHistory;

public class GetPaymentHistoryQueryHandler : IRequestHandler<GetPaymentHistoryQuery, Result<List<PaymentHistoryResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPaymentHistoryQueryHandler> _logger;

    public GetPaymentHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetPaymentHistoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<PaymentHistoryResponse>>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<List<PaymentHistoryResponse>>.Failure(
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
                return Result<List<PaymentHistoryResponse>>.Failure(
                    "User not found",
                    ErrorCode.NotFound);
            }

            // Get payment history with transaction details
            var paymentHistory = await _unitOfWork.PaymentRepository
                .GetPaymentHistoryByUserIdAsync(request.UserId);

            // Map to response
            var response = _mapper.Map<List<PaymentHistoryResponse>>(paymentHistory);

            return Result<List<PaymentHistoryResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history for user ID: {UserId}", request.UserId);
            return Result<List<PaymentHistoryResponse>>.Failure(
                "Error getting payment history",
                ErrorCode.InternalError);
        }
    }
} 