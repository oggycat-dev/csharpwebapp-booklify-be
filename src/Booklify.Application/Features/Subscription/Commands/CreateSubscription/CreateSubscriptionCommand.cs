using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Commands.CreateSubscription;

/// <summary>
/// Command to create a new subscription plan
/// </summary>
public record CreateSubscriptionCommand : IRequest<Result<SubscriptionResponse>>
{
    public CreateSubscriptionRequest Request { get; init; } = new();
} 