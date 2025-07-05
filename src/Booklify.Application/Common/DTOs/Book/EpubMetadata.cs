namespace Booklify.Application.Common.DTOs.Book;

public class EpubMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string CoverImagePath { get; set; } = string.Empty;
    public byte[]? CoverImageContent { get; set; }
    public string CoverImageMimeType { get; set; } = string.Empty;
}
