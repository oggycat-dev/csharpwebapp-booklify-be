using MediatR;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.BookCategory.Queries.GetBookCategories;

/// <summary>
/// Query to get a list of book categories with filtering and pagination
/// </summary>
public record GetBookCategoriesQuery(BookCategoryFilterModel? Filter) : IRequest<PaginatedResult<BookCategoryResponse>>; 