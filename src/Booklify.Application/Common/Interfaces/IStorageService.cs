namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Service interface for file storage operations
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">Content type of the file</param>
    /// <param name="folder">Optional folder path</param>
    /// <returns>Relative path of the uploaded file</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null);
    
    /// <summary>
    /// Upload a file from byte array to storage
    /// </summary>
    /// <param name="fileBytes">File bytes to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">Content type of the file</param>
    /// <param name="folder">Optional folder path</param>
    /// <returns>Relative path of the uploaded file</returns>
    Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string? folder = null);
    
    /// <summary>
    /// Upload EPUB file with specific book category prefix
    /// </summary>
    /// <param name="fileStream">EPUB file stream to upload</param>
    /// <param name="fileName">Name of the EPUB file</param>
    /// <param name="contentType">Content type (should be application/epub+zip)</param>
    /// <param name="categoryName">Optional book category name for folder structure</param>
    /// <returns>Relative path of the uploaded EPUB file</returns>
    Task<string> UploadEpubFileAsync(Stream fileStream, string fileName, string contentType, string? categoryName = null);
    
    /// <summary>
    /// Upload EPUB file from byte array with specific book category prefix
    /// </summary>
    /// <param name="fileBytes">EPUB file bytes to upload</param>
    /// <param name="fileName">Name of the EPUB file</param>
    /// <param name="contentType">Content type (should be application/epub+zip)</param>
    /// <param name="categoryName">Optional book category name for folder structure</param>
    /// <returns>Relative path of the uploaded EPUB file</returns>
    Task<string> UploadEpubFileAsync(byte[] fileBytes, string fileName, string contentType, string? categoryName = null);
    
    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="fileUrl">URL or relative path of the file to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string fileUrl);
    
    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    /// <param name="fileUrl">URL or relative path of the file</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string fileUrl);
    
    /// <summary>
    /// Get the public URL for a file from its relative path
    /// </summary>
    /// <param name="filePath">Relative path of the file</param>
    /// <returns>Public URL of the file</returns>
    string GetFileUrl(string filePath);
    
    /// <summary>
    /// Download a file from storage
    /// </summary>
    /// <param name="fileUrl">URL or relative path of the file</param>
    /// <returns>File stream</returns>
    Task<Stream?> DownloadFileAsync(string fileUrl);
    
    /// <summary>
    /// Get file information
    /// </summary>
    /// <param name="fileUrl">URL or relative path of the file</param>
    /// <returns>File metadata</returns>
    Task<StorageFileInfo?> GetFileInfoAsync(string fileUrl);
}

/// <summary>
/// Storage file information
/// </summary>
public class StorageFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string Url { get; set; } = string.Empty;
} 