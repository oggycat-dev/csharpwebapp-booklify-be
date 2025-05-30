namespace Booklify.Domain.Commons;

/// <summary>
/// Base entity class with audit properties
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// ID of user who created the entity
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
    
    /// <summary>
    /// ID of user who last modified the entity
    /// </summary>
    public Guid? ModifiedBy { get; set; }
    
    /// <summary>
    /// Soft deletion timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// ID of user who deleted the entity
    /// </summary>
    public Guid? DeletedBy { get; set; }
    
    /// <summary>
    /// Soft deletion flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;
} 