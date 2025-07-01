using MediatR;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Commands.IncrementBookViews;

/// <summary>
/// Command để tăng lượt xem của sách
/// </summary>
public record IncrementBookViewsCommand(Guid BookId) : IRequest<Result>; 