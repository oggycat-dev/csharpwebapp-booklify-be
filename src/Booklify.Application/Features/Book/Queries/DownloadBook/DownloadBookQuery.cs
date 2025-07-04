using MediatR;
using Booklify.Application.Common.Models;

namespace Booklify.Application.Features.Book.Queries.DownloadBook;

/// <summary>
/// Query để download file sách
/// </summary>
/// <param name="BookId">ID của sách cần download</param>
public record DownloadBookQuery(Guid BookId) : IRequest<Result<BookDownloadResponse>>;

/// <summary>
/// Response chứa thông tin file để download
/// </summary>
public class BookDownloadResponse
{
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
} 