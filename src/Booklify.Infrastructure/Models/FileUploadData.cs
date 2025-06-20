namespace Booklify.Infrastructure.Models;

/// <summary>
/// Data model for file upload operations
/// </summary>
public class FileUploadData
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
} 