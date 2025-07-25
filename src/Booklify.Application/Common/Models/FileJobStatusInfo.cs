using Booklify.Domain.Enums;

namespace Booklify.Application.Common.Models;

/// <summary>
/// Information about a background file job status
/// </summary>
public class FileJobStatusInfo
{
    public string JobId { get; set; } = string.Empty;
    public FileJobStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StateDisplayName { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : null;
} 