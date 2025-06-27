using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Book.Commands.DeleteBook;

/// <summary>
/// Command để soft delete sách
/// </summary>
public record DeleteBookCommand(Guid BookId) : IRequest<Result>; 