using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.GetBookStatistics;

/// <summary>
/// Query để lấy thống kê sách
/// </summary>
public record GetBookStatisticsQuery() : IRequest<Result<BookStatisticsResponse>>; 