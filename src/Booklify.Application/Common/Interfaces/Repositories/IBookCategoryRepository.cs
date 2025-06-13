using Booklify.Domain.Entities;
using Booklify.Application.Common.DTOs.BookCategory;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface IBookCategoryRepository : IGenericRepository<BookCategory>
{
    Task<(List<BookCategory> BookCategories, int TotalCount)> GetPagedBookCategoriesAsync(BookCategoryFilterModel filter);
} 