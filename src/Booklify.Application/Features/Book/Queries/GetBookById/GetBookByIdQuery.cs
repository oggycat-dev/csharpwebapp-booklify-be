using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.GetBookById;

/// <summary>
/// Query để lấy thông tin chi tiết sách theo ID
/// </summary>
public record GetBookByIdQuery(Guid BookId) : IRequest<Result<BookResponse>>; 