using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Queries.GetAllSubscriptions;

public class GetAllSubscriptionsQueryHandler : IRequestHandler<GetAllSubscriptionsQuery, Result<PaginatedResult<SubscriptionResponse>>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IMapper _mapper;

    public GetAllSubscriptionsQueryHandler(IBooklifyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResult<SubscriptionResponse>>> Handle(GetAllSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Subscriptions.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Filter.Name))
        {
            query = query.Where(s => s.Name.ToLower().Contains(request.Filter.Name.ToLower()));
        }

        if (request.Filter.MinPrice.HasValue)
        {
            query = query.Where(s => s.Price >= request.Filter.MinPrice.Value);
        }

        if (request.Filter.MaxPrice.HasValue)
        {
            query = query.Where(s => s.Price <= request.Filter.MaxPrice.Value);
        }

        if (request.Filter.MinDuration.HasValue)
        {
            query = query.Where(s => s.Duration >= request.Filter.MinDuration.Value);
        }

        if (request.Filter.MaxDuration.HasValue)
        {
            query = query.Where(s => s.Duration <= request.Filter.MaxDuration.Value);
        }

        if (request.Filter.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Filter.Status.Value);
        }
        else
        {
            // By default, only show active subscriptions
            query = query.Where(s => s.Status == EntityStatus.Active);
        }

        if (request.Filter.IsPopular.HasValue)
        {
            query = query.Where(s => s.IsPopular == request.Filter.IsPopular.Value);
        }

        // Apply search
        if (!string.IsNullOrEmpty(request.Filter.SearchKeyword))
        {
            query = query.Where(s => s.Name.ToLower().Contains(request.Filter.SearchKeyword.ToLower()) ||
                                   (s.Description != null && s.Description.ToLower().Contains(request.Filter.SearchKeyword.ToLower())));
        }

        // Apply sorting
        query = request.Filter.SortBy?.ToLower() switch
        {
            "name" => request.Filter.IsDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "price" => request.Filter.IsDescending ? query.OrderByDescending(s => s.Price) : query.OrderBy(s => s.Price),
            "duration" => request.Filter.IsDescending ? query.OrderByDescending(s => s.Duration) : query.OrderBy(s => s.Duration),
            "createdat" => request.Filter.IsDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            "displayorder" => request.Filter.IsDescending ? query.OrderByDescending(s => s.DisplayOrder) : query.OrderBy(s => s.DisplayOrder),
            _ => query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.CreatedAt)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var subscriptions = await query
            .Skip((request.Filter.PageNumber - 1) * request.Filter.PageSize)
            .Take(request.Filter.PageSize)
            .ToListAsync(cancellationToken);

        var subscriptionResponses = _mapper.Map<List<SubscriptionResponse>>(subscriptions);

        var paginatedResult = PaginatedResult<SubscriptionResponse>.Success(
            subscriptionResponses,
            request.Filter.PageNumber,
            request.Filter.PageSize,
            totalCount);

        return Result<PaginatedResult<SubscriptionResponse>>.Success(paginatedResult, "Subscription plans retrieved successfully");
    }
} 