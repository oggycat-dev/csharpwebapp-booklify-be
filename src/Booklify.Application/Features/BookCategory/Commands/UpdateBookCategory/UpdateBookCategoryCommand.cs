using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.BookCategory.Commands.UpdateBookCategory;

public record UpdateBookCategoryCommand(Guid CategoryId, UpdateBookCategoryRequest Request) : IRequest<Result<BookCategoryResponse>>; 