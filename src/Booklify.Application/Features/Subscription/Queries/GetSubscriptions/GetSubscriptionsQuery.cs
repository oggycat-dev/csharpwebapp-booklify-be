using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Queries.GetSubscriptions;

public record GetSubscriptionsQuery : IRequest<Result<List<SubscriptionResponse>>>; 