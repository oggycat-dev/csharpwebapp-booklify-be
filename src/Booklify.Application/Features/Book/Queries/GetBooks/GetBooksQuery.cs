using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.GetBooks;

/// <summary>
/// Query để lấy danh sách sách cho admin view với đầy đủ filter (trả về thông tin cơ bản)
/// </summary>
public record GetBooksQuery(BookFilterModel Filter) : IRequest<Result<PaginatedResult<BookListItemResponse>>>; 