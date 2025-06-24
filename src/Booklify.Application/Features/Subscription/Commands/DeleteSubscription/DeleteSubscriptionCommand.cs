using MediatR;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Commands.DeleteSubscription;

/// <summary>
/// Command to delete (soft delete) a subscription plan
/// </summary>
public record DeleteSubscriptionCommand(Guid Id) : IRequest<Result>; 