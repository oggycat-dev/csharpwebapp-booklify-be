using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

public class FileInfo : BaseEntity
{
    public string FilePath { get; set; } = string.Empty;
    public string ServerUpload { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public double SizeKb { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    
    // Job-related fields for background processing
    public FileJobStatus JobStatus { get; set; } = FileJobStatus.Pending;
    public DateTime? JobStartedAt { get; set; }
    public DateTime? JobCompletedAt { get; set; }
    public string? JobErrorMessage { get; set; }
}