using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Queries.GetSubscriptionById;

/// <summary>
/// Query to get a subscription plan by ID
/// </summary>
public record GetSubscriptionByIdQuery(Guid Id) : IRequest<Result<SubscriptionResponse>>; 