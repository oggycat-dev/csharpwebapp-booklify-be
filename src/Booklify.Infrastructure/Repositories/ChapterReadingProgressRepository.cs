using Booklify.Application.Common.Interfaces;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class ChapterReadingProgressRepository : GenericRepository<ChapterReadingProgress>, IChapterReadingProgressRepository
{
    public ChapterReadingProgressRepository(BooklifyDbContext context) : base(context)
    {
    }
}