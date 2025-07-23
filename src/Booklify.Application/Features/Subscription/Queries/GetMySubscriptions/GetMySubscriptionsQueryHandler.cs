using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Queries.GetMySubscriptions;

public class GetMySubscriptionsQueryHandler : IRequestHandler<GetMySubscriptionsQuery, Result<List<UserSubscriptionResponse>>>
{
    private readonly IBooklifyDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMySubscriptionsQueryHandler(
        IBooklifyDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<List<UserSubscriptionResponse>>> Handle(GetMySubscriptionsQuery request, CancellationToken cancellationToken)
    {
        // Get current user
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Result<List<UserSubscriptionResponse>>.Failure("User is not authenticated", ErrorCode.Unauthorized);
        }

        // Find user profile
        var userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.IdentityUserId == currentUserId, cancellationToken);

        if (userProfile == null)
        {
            return Result<List<UserSubscriptionResponse>>.Failure("User profile not found", ErrorCode.UserNotFound);
        }

        // Update expired subscriptions automatically
        var expiredSubscriptions = await _context.UserSubscriptions
            .Where(us => us.UserId == userProfile.Id &&
                        us.Status == EntityStatus.Active &&
                        us.EndDate <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var expiredSub in expiredSubscriptions)
        {
            expiredSub.Status = EntityStatus.Expired;
        }

        if (expiredSubscriptions.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Get user subscriptions with related data
        var userSubscriptions = await _context.UserSubscriptions
            .Include(us => us.Subscription)
            .Include(us => us.Payments)
            .Where(us => us.UserId == userProfile.Id)
            .OrderByDescending(us => us.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = _mapper.Map<List<UserSubscriptionResponse>>(userSubscriptions);
        
        return Result<List<UserSubscriptionResponse>>.Success(response);
    }
} 