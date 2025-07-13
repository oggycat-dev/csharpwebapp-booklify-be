using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Commands.ManageSubscription;

/// <summary>
/// Command to manage user subscription (extend, cancel, gift, toggle auto-renew, re-subscription)
/// </summary>
public class ManageSubscriptionCommand : IRequest<Result<SubscriptionManagementResponse>>
{
    public Guid UserId { get; set; }
    public SubscriptionManagementRequest Request { get; set; }
    
    public ManageSubscriptionCommand(Guid userId, SubscriptionManagementRequest request)
    {
        UserId = userId;
        Request = request;
    }
} 