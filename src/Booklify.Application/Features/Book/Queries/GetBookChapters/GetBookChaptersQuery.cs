using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.GetBookChapters;

public class GetBookChaptersQuery : IRequest<Result<List<ChapterResponse>>>
{
    public Guid BookId { get; set; }
    
    public GetBookChaptersQuery(Guid bookId)
    {
        BookId = bookId;
    }
}
