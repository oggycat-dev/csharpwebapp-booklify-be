using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Queries.GetAllSubscriptions;

/// <summary>
/// Query to get all subscription plans with filtering
/// </summary>
public record GetAllSubscriptionsQuery : IRequest<Result<PaginatedResult<SubscriptionResponse>>>
{
    public SubscriptionFilterModel Filter { get; init; } = new();
} 