using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.GetBooks;

/// <summary>
/// Query để lấy danh sách sách với phân trang và lọc
/// </summary>
public record GetBooksQuery(BookFilterModel? Filter) : IRequest<PaginatedResult<BookResponse>>; 