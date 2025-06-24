using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class FileInfoRepository : GenericRepository<Domain.Entities.FileInfo>, IFileInfoRepository
{
    public FileInfoRepository(BooklifyDbContext context) : base(context)
    {
    }
} 