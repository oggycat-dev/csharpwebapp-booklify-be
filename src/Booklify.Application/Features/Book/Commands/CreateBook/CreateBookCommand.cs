using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Commands.CreateBook;
 
public record CreateBookCommand(CreateBookRequest Request) : IRequest<Result<BookResponse>>; 