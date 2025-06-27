using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Book.Commands.ManageBookStatus;

public record ManageBookStatusCommand(Guid BookId, ManageBookStatusRequest Request) : IRequest<Result<BookResponse>>; 