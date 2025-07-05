namespace Booklify.Application.Common.DTOs.Book;

/// <summary>
/// DTO for EPUB metadata extraction
/// </summary>
public class EpubMetadataDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte[]? CoverImageBytes { get; set; }
    public int TotalPages { get; set; }
    public DateTime? PublishedDate { get; set; }
}
