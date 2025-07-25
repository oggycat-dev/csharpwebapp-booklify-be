using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Queries.GetSubscriptionById;

public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Result<SubscriptionResponse>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IMapper _mapper;

    public GetSubscriptionByIdQueryHandler(IBooklifyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<SubscriptionResponse>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.Status == EntityStatus.Active, cancellationToken);

        if (subscription == null)
        {
            return Result<SubscriptionResponse>.Failure("Subscription plan not found", ErrorCode.NotFound);
        }

        var response = _mapper.Map<SubscriptionResponse>(subscription);
        return Result<SubscriptionResponse>.Success(response, "Subscription plan retrieved successfully");
    }
} 