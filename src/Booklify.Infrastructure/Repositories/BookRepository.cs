using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(BooklifyDbContext context) : base(context)
    {
    }
}