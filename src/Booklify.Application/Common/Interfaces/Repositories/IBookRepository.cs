using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Domain.Entities;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface IBookRepository : IGenericRepository<Book>
{
    Task<(List<Book> Books, int TotalCount)> GetPagedBooksAsync(BookFilterModel filter);
}
