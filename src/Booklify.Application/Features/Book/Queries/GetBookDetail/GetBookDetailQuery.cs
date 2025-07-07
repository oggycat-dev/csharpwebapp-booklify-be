using MediatR;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.GetBookDetail;

public class GetBookDetailQuery : IRequest<Result<BookDetailResponse>>
{
    public Guid BookId { get; set; }
    
    public GetBookDetailQuery(Guid bookId)
    {
        BookId = bookId;
    }
}
