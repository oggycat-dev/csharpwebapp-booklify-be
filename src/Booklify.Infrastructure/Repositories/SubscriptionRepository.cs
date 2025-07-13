using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booklify.Infrastructure.Repositories;

public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetSubscriptionByIdAsync(Guid id)
    {
        return await GetByIdAsync(id);
    }
} 