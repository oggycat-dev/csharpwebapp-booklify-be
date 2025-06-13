using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Repositories;

public class BookCategoryRepository : GenericRepository<BookCategory>, IBookCategoryRepository
{
    public BookCategoryRepository(BooklifyDbContext context) : base(context)
    {
    }
} 