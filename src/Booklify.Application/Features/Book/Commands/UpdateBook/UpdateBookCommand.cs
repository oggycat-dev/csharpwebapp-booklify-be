using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Book.Commands.UpdateBook;

public record UpdateBookCommand(Guid BookId, UpdateBookRequest Request) : IRequest<Result<BookResponse>>; 