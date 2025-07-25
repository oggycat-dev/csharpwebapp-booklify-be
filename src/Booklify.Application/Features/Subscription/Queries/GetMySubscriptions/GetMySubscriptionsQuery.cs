using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Queries.GetMySubscriptions;

public record GetMySubscriptionsQuery : IRequest<Result<List<UserSubscriptionResponse>>>; 