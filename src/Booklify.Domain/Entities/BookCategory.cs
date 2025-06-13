using Booklify.Domain.Commons;
using Booklify.Domain.Enums;

namespace Booklify.Domain.Entities;

public class BookCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public virtual ICollection<Book>? Books { get; set; }
}