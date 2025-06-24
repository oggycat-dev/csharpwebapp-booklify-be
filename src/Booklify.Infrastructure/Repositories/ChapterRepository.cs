using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
{
    public ChapterRepository(BooklifyDbContext context) : base(context)
    {
    }
} 