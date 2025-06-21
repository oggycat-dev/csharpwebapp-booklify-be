using MediatR;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Subscription.Commands.UpdateSubscription;

/// <summary>
/// Command to update an existing subscription plan
/// </summary>
public record UpdateSubscriptionCommand : IRequest<Result<SubscriptionResponse>>
{
    public Guid Id { get; init; }
    public UpdateSubscriptionRequest Request { get; init; } = new();
} 