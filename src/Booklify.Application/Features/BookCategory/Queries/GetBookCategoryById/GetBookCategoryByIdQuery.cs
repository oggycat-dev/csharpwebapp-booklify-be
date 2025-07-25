using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.BookCategory.Queries.GetBookCategoryById;

public record GetBookCategoryByIdQuery(Guid CategoryId) : IRequest<Result<BookCategoryResponse>>; 