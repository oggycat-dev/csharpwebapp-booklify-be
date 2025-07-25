using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.Book.Commands.ResubmitBook;

public record ResubmitBookCommand(Guid BookId, ResubmitBookRequest Request) : IRequest<Result>;
