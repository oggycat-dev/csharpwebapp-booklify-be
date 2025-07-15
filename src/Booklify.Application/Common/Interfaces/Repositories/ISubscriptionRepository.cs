using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Domain.Entities;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface ISubscriptionRepository : IGenericRepository<Subscription>
{
    Task<Subscription?> GetSubscriptionByIdAsync(Guid id);
    Task<(List<Subscription> Subscriptions, int TotalCount)> GetPagedSubscriptionsAsync(SubscriptionFilterModel filter);
}

public interface IUserSubscriptionRepository : IGenericRepository<UserSubscription>
{
    Task<List<UserSubscription>> GetSubscriptionHistoryByUserIdAsync(Guid userId);
    Task<UserSubscription?> GetActiveSubscriptionByUserIdAsync(Guid userId);
} 