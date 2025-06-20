using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

public class Chapter : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Href { get; set; }
    public string? Cfi { get; set; }
    public Guid? ParentChapterId { get; set; }
    public Guid? BookId { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    
    // Navigation Properties
    public virtual Chapter? ParentChapter { get; set; }
    public virtual ICollection<Chapter> ChildChapters { get; set; } = new List<Chapter>();
    public virtual Book? Book { get; set; }
} 