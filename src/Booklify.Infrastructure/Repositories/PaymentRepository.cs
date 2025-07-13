using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booklify.Infrastructure.Repositories;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<List<Payment>> GetPaymentHistoryByUserIdAsync(Guid userId)
    {
        return await FindByCondition(
            p => p.UserSubscription.UserId == userId,
            p => p.PaymentDate,
            ascending: false,
            p => p.UserSubscription!,
            p => p.UserSubscription!.Subscription!)
            .ToListAsync();
    }
} 