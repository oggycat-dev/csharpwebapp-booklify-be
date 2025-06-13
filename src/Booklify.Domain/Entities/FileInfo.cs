using Booklify.Domain.Commons;
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
}