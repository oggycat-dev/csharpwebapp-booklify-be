using Booklify.Application.Common.DTOs.Book;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Interface for EPUB processing service
/// </summary>
public interface IEPubService
{
    /// <summary>
    /// Extract chapters from an EPUB file
    /// </summary>
    Task<(List<Chapter> chapters, int totalCount)> ExtractChapters(string epubFilePath);

    /// <summary>
    /// Extract metadata from an EPUB file
    /// </summary>
    Task<EpubMetadataDto> ExtractMetadataAsync(string epubFilePath);

    /// <summary>
    /// Process an EPUB file with pre-downloaded content and queue background operations
    /// </summary>
    string ProcessEpubFileWithContent(Guid bookId, string userId, byte[] fileContent, string fileExtension);
} 