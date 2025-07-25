using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class ReadingProgressRepository : GenericRepository<ReadingProgress>, IReadingProgressRepository
{
    public ReadingProgressRepository(BooklifyDbContext context) : base(context)
    {
    }
}
