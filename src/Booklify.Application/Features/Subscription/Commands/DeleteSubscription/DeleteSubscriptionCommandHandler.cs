using MediatR;
using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Commands.DeleteSubscription;

public class DeleteSubscriptionCommandHandler : IRequestHandler<DeleteSubscriptionCommand, Result>
{
    private readonly IBooklifyDbContext _context;

    public DeleteSubscriptionCommandHandler(IBooklifyDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Find subscription
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.Status == EntityStatus.Active, cancellationToken);

        if (subscription == null)
        {
            return Result.Failure("Subscription plan not found", ErrorCode.NotFound);
        }

        // Check if subscription has active user subscriptions
        var hasActiveSubscriptions = await _context.UserSubscriptions
            .AnyAsync(us => us.SubscriptionId == request.Id && 
                          us.IsActive && 
                          us.Status == EntityStatus.Active, cancellationToken);

        if (hasActiveSubscriptions)
        {
            return Result.Failure("Cannot delete subscription plan that has active subscribers", ErrorCode.BusinessRuleViolation);
        }

        // Soft delete subscription
        subscription.Status = EntityStatus.Inactive;
        subscription.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Subscription plan deleted successfully");
    }
} 