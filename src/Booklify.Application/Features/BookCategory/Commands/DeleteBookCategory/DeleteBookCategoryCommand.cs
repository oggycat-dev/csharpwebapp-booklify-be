using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.BookCategory.Commands.DeleteBookCategory;

public record DeleteBookCategoryCommand(Guid CategoryId) : IRequest<Result>; 