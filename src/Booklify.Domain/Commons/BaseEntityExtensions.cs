namespace Booklify.Domain.Commons;

/// <summary>
/// Extension methods for BaseEntity
/// </summary>
public static class BaseEntityExtensions
{
    #region Single Entity Methods
    /// <summary>
    /// Initialize base values for a new entity
    /// </summary>
    /// <param name="entity">Entity to initialize</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void InitializeBaseEntity(this BaseEntity entity, string userId)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = userId != null ? Guid.Parse(userId) : null;
        entity.ModifiedAt = null;
        entity.ModifiedBy = null;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.IsDeleted = false;
    }

    /// <summary>
    /// Initialize base values for a new entity using Guid
    /// </summary>
    /// <param name="entity">Entity to initialize</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void InitializeBaseEntity(this BaseEntity entity, Guid? userId)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = userId;
        entity.ModifiedAt = null;
        entity.ModifiedBy = null;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.IsDeleted = false;
    }

    /// <summary>
    /// Update modification information for an entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void UpdateBaseEntity(this BaseEntity entity, string userId)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = userId != null ? Guid.Parse(userId) : null;
    }

    /// <summary>
    /// Update modification information for an entity using Guid
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void UpdateBaseEntity(this BaseEntity entity, Guid? userId)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = userId;
    }

    /// <summary>
    /// Mark entity as deleted (soft delete)
    /// </summary>
    /// <param name="entity">Entity to delete</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void SoftDelete(this BaseEntity entity, string userId)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = userId != null ? Guid.Parse(userId) : null;
    }

    /// <summary>
    /// Mark entity as deleted (soft delete) using Guid
    /// </summary>
    /// <param name="entity">Entity to delete</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void SoftDelete(this BaseEntity entity, Guid? userId)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = userId;
    }

    /// <summary>
    /// Restore a soft-deleted entity
    /// </summary>
    /// <param name="entity">Entity to restore</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void Restore(this BaseEntity entity, string userId)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = userId != null ? Guid.Parse(userId) : null;
    }

    /// <summary>
    /// Restore a soft-deleted entity using Guid
    /// </summary>
    /// <param name="entity">Entity to restore</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void Restore(this BaseEntity entity, Guid? userId)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = userId;
    }
    #endregion

    #region Collection Methods
    /// <summary>
    /// Initialize base values for multiple entities at once
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to initialize</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void InitializeBaseEntities<T>(this IEnumerable<T> entities, string userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.InitializeBaseEntity(userId);
        }
    }

    /// <summary>
    /// Initialize base values for multiple entities at once using Guid
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to initialize</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void InitializeBaseEntities<T>(this IEnumerable<T> entities, Guid? userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.InitializeBaseEntity(userId);
        }
    }

    /// <summary>
    /// Update modification information for multiple entities at once
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to update</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void UpdateBaseEntities<T>(this IEnumerable<T> entities, string userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.UpdateBaseEntity(userId);
        }
    }

    /// <summary>
    /// Update modification information for multiple entities at once using Guid
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to update</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void UpdateBaseEntities<T>(this IEnumerable<T> entities, Guid? userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.UpdateBaseEntity(userId);
        }
    }

    /// <summary>
    /// Mark multiple entities as deleted (soft delete) at once
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to delete</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void SoftDeleteEntities<T>(this IEnumerable<T> entities, string userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.SoftDelete(userId);
        }
    }

    /// <summary>
    /// Mark multiple entities as deleted (soft delete) at once using Guid
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to delete</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void SoftDeleteEntities<T>(this IEnumerable<T> entities, Guid? userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.SoftDelete(userId);
        }
    }

    /// <summary>
    /// Restore multiple soft-deleted entities at once
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to restore</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void RestoreEntities<T>(this IEnumerable<T> entities, string userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.Restore(userId);
        }
    }

    /// <summary>
    /// Restore multiple soft-deleted entities at once using Guid
    /// </summary>
    /// <typeparam name="T">Entity type inheriting from BaseEntity</typeparam>
    /// <param name="entities">List of entities to restore</param>
    /// <param name="userId">ID of user performing the action</param>
    public static void RestoreEntities<T>(this IEnumerable<T> entities, Guid? userId) where T : BaseEntity
    {
        foreach (var entity in entities)
        {
            entity.Restore(userId);
        }
    }
    #endregion
} 