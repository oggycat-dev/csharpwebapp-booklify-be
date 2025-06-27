using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class ChapterAIResultRepository : GenericRepository<ChapterAIResult>, IChapterAIResultRepository
{
    public ChapterAIResultRepository(BooklifyDbContext context) : base(context)
    {
    }
} 