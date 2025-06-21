using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Queries.GetSubscriptions;

public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, Result<List<SubscriptionResponse>>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IMapper _mapper;

    public GetSubscriptionsQueryHandler(IBooklifyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<SubscriptionResponse>>> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subscriptions = await _context.Subscriptions
            .Where(s => s.Status == EntityStatus.Active)
            .OrderBy(s => s.Price)
            .ToListAsync(cancellationToken);

        var response = _mapper.Map<List<SubscriptionResponse>>(subscriptions);
        
        return Result<List<SubscriptionResponse>>.Success(response);
    }
} 