using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.BookCategory.Commands.CreateBookCategory;

public record CreateBookCategoryCommand(CreateBookCategoryRequest Request) : IRequest<Result<CreatedBookCategoryResponse>>; 