using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
using Booklify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booklify.Infrastructure.Repositories;

public class UserSubscriptionRepository : GenericRepository<UserSubscription>, IUserSubscriptionRepository
{
    public UserSubscriptionRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<List<UserSubscription>> GetSubscriptionHistoryByUserIdAsync(Guid userId)
    {
        return await FindByCondition(
            us => us.UserId == userId,
            us => us.CreatedAt,
            ascending: false,
            us => us.Subscription!)
            .ToListAsync();
    }

    public async Task<UserSubscription?> GetActiveSubscriptionByUserIdAsync(Guid userId)
    {
        return await GetFirstOrDefaultAsync(
            us => us.UserId == userId && 
                  us.Status == EntityStatus.Active && 
                  us.EndDate > DateTime.UtcNow,
            us => us.Subscription!);
    }
} 