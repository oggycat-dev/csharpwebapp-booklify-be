using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public string? CoverImageUrl { get; set; }
    public bool IsPremium { get; set; }
    public string? FilePath { get; set; }
    public string? Tags { get; set; }
    public int PageCount { get; set; } = 0;
    public DateTime? PublishedDate { get; set; }
    
    //Navigator Properties
    public virtual BookCategory? Category { get; set; }
    public virtual FileInfo? File { get; set; }
    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}