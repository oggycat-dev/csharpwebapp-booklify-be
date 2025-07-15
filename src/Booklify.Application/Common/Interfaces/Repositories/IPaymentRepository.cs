using Booklify.Domain.Entities;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<List<Payment>> GetPaymentHistoryByUserIdAsync(Guid userId);
} 