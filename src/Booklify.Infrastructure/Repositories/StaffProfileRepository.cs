using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;
public class StaffProfileRepository : GenericRepository<StaffProfile>, IStaffProfileRepository
{
    public StaffProfileRepository(BooklifyDbContext context) : base(context)
    {
    }
}   