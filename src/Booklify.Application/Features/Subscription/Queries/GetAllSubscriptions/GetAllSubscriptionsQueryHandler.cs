using MediatR;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.Subscription.Queries.GetAllSubscriptions;

public class GetAllSubscriptionsQueryHandler : IRequestHandler<GetAllSubscriptionsQuery, Result<PaginatedResult<SubscriptionResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllSubscriptionsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResult<SubscriptionResponse>>> Handle(GetAllSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        // Use repository to get paged subscriptions with filtering
        var (subscriptions, totalCount) = await _unitOfWork.SubscriptionRepository.GetPagedSubscriptionsAsync(request.Filter);

        var subscriptionResponses = _mapper.Map<List<SubscriptionResponse>>(subscriptions);

        var paginatedResult = PaginatedResult<SubscriptionResponse>.Success(
            subscriptionResponses,
            request.Filter.PageNumber,
            request.Filter.PageSize,
            totalCount);

        return Result<PaginatedResult<SubscriptionResponse>>.Success(paginatedResult, "Subscription plans retrieved successfully");
    }
} 