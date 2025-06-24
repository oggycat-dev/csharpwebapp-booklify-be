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
    Task<List<Chapter>> ExtractChapters(string epubFilePath);

    /// <summary>
    /// Process an EPUB file and queue background operations
    /// </summary>
    string ProcessEpubFile(Guid bookId, string userId = "", FileUploadType uploadType = FileUploadType.None);
} 