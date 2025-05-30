using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Commons;
using Booklify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booklify.Infrastructure.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
{
    private readonly BooklifyDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(BooklifyDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    // Add GetEntityId helper method
    protected virtual object GetEntityId(TEntity entity)
    {
        var keyProperty = _context.Model.FindEntityType(typeof(TEntity))
            ?.FindPrimaryKey()?.Properties[0];

        if (keyProperty == null)
            throw new InvalidOperationException("Entity does not have a primary key");

        return keyProperty.GetGetter().GetClrValue(entity);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var entry = await _dbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var entityState = _context.Entry(entity).State;
        if (entityState == EntityState.Detached)
        {
            var id = GetEntityId(entity);
            var trackedEntity = await _dbSet.FindAsync(id);
            if (trackedEntity == null)
                throw new KeyNotFoundException($"Entity with id {id} not found");

            _dbSet.Remove(trackedEntity);
        }
        else
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression)
    {
        return _dbSet.AsNoTracking().Where(expression);
    }

    public virtual IQueryable<TEntity> FindByCondition(
        Expression<Func<TEntity, bool>> expression,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsNoTracking().Where(expression);

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        return query;
    }

    public virtual IQueryable<TEntity> FindByCondition<TKey>(
        Expression<Func<TEntity, bool>> expression,
        Expression<Func<TEntity, TKey>> orderBy,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsNoTracking().Where(expression);

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        return ascending
            ? query.OrderBy(orderBy)
            : query.OrderByDescending(orderBy);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync<TKey>(Expression<Func<TEntity, TKey>> orderBy, bool ascending = true)
    {
        return ascending
            ? await _dbSet.AsNoTracking().OrderBy(orderBy).ToListAsync()
            : await _dbSet.AsNoTracking().OrderByDescending(orderBy).ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsNoTracking();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync<TKey>(
        Expression<Func<TEntity, TKey>> orderBy,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsNoTracking();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        return ascending
            ? await query.OrderBy(orderBy).ToListAsync()
            : await query.OrderByDescending(orderBy).ToListAsync();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
    {
        return await GetByIdAsync((object)id, includes);
    }

    public virtual async Task<TEntity?> GetByIdAsync(object id, params Expression<Func<TEntity, object>>[] includes)
    {
        // If no includes, use FindAsync
        if (includes == null || !includes.Any())
        {
            return await _dbSet.FindAsync(id);
        }

        // If includes exist, use FirstOrDefaultAsync with includes
        var keyProperty = _context.Model.FindEntityType(typeof(TEntity))
            ?.FindPrimaryKey()?.Properties[0];

        if (keyProperty == null)
            throw new InvalidOperationException("Entity does not have a primary key");

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(parameter, keyProperty.Name);
        var constant = Expression.Constant(id);
        var equality = Expression.Equal(property, Expression.Convert(constant, keyProperty.ClrType));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equality, parameter);

        return await GetFirstOrDefaultAsync(lambda, includes);
    }

    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }
        return await query.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var id = GetEntityId(entity);
        var existingEntity = await _dbSet.FindAsync(id);

        if (existingEntity == null)
            throw new KeyNotFoundException($"Entity with id {id} not found");

        _context.Entry(existingEntity).CurrentValues.SetValues(entity);

        foreach (var navigation in _context.Entry(existingEntity).Navigations)
        {
            if (navigation.Metadata.IsCollection)
                continue;

            var navigationValue = navigation.Metadata.PropertyInfo?.GetValue(entity);
            if (navigationValue != null)
            {
                navigation.CurrentValue = navigationValue;
            }
        }

        _context.Entry(existingEntity).State = EntityState.Modified;
        return existingEntity;
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    // AddRangeAsync - already optimized
    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        await _dbSet.AddRangeAsync(entities);
        return entities;
    }

    // UpdateRangeAsync - Optimized version
    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return;

        // For tracked entities, just call UpdateRange
        var trackedEntities = entitiesList.Where(e => _context.Entry(e).State != EntityState.Detached).ToList();
        if (trackedEntities.Any())
        {
            _dbSet.UpdateRange(trackedEntities);
        }

        // For detached entities, attach and mark as modified
        var detachedEntities = entitiesList.Where(e => _context.Entry(e).State == EntityState.Detached).ToList();
        foreach (var entity in detachedEntities)
        {
            var entry = _context.Entry(entity);
            entry.State = EntityState.Modified;
        }
    }

    // UpdateRangeAsync - Alternative optimized version for entities from AsNoTracking queries
    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, bool fromNoTracking = false)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return;

        if (fromNoTracking)
        {
            // For entities from AsNoTracking queries, we need to attach them first
            foreach (var entity in entitiesList)
            {
                var entry = _context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    _dbSet.Attach(entity);
                    entry.State = EntityState.Modified;
                }
            }
        }
        else
        {
            // Use the standard UpdateRange for tracked entities
            _dbSet.UpdateRange(entitiesList);
        }
    }

    // DeleteRangeAsync - Optimized version
    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return;

        // Attach and remove in one go for better performance
        _dbSet.RemoveRange(entitiesList);
    }

    // DeleteRangeAsync - Alternative version for detached entities
    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, bool fromNoTracking = false)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
            return;

        // For better performance, just attach and remove in one go
        _dbSet.RemoveRange(entitiesList);
    }

    // SoftDeleteRangeAsync - Optimized version
    public virtual async Task<IEnumerable<TEntity>> SoftDeleteRangeAsync(IEnumerable<TEntity> entities, string userId)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entitiesList = entities.Where(e => e is BaseEntity).Cast<BaseEntity>().ToList();
        if (!entitiesList.Any())
            return Enumerable.Empty<TEntity>();

        // Batch update all entities at once
        var now = DateTime.UtcNow;
        var userGuid = userId != null ? Guid.Parse(userId) : (Guid?)null;

        foreach (var entity in entitiesList)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = userGuid;
            _context.Entry(entity).State = EntityState.Modified;
        }

        return entitiesList.Cast<TEntity>();
    }

    // SoftDeleteByConditionAsync - Optimized bulk soft delete by condition
    public virtual async Task<int> SoftDeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, string userId)
    {
        if (!typeof(BaseEntity).IsAssignableFrom(typeof(TEntity)))
            throw new InvalidOperationException("Entity does not inherit from BaseEntity");

        var now = DateTime.UtcNow;
        var userGuid = userId != null ? Guid.Parse(userId) : (Guid?)null;

        // Use a single query to update all matching records
        var entities = await _dbSet.Where(predicate).ToListAsync();
        
        if (!entities.Any())
            return 0;

        foreach (var entity in entities.Cast<BaseEntity>())
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = userGuid;
            _context.Entry(entity).State = EntityState.Modified;
        }

        return entities.Count;
    }

    // RestoreRangeAsync - New method for bulk restore
    public virtual async Task<IEnumerable<TEntity>> RestoreRangeAsync(IEnumerable<TEntity> entities, string userId)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entitiesList = entities.Where(e => e is BaseEntity).ToList();
        if (!entitiesList.Any())
            return entitiesList;

        foreach (var entity in entitiesList)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.Restore(userId);
            }
        }

        await UpdateRangeAsync(entitiesList);
        return entitiesList;
    }

    // FindAsyncForUpdate - Special method for entities intended for updates (avoids AsNoTracking)
    public virtual async Task<IEnumerable<TEntity>> FindAsyncForUpdate(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsQueryable(); // Don't use AsNoTracking for updates

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        return await query.Where(predicate).ToListAsync();
    }

    public virtual IQueryable<TEntity> FindByConditionIncludeDeleted(Expression<Func<TEntity, bool>> expression)
    {
        // Create a new DbSet without the filter
        var dbSetWithoutFilter = _context.Set<TEntity>().IgnoreQueryFilters();
        return dbSetWithoutFilter.AsNoTracking().Where(expression);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllIncludeDeletedAsync()
    {
        return await _context.Set<TEntity>().IgnoreQueryFilters().AsNoTracking().ToListAsync();
    }

    public virtual async Task<TEntity?> GetByIdIncludeDeletedAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
    {
        // If no includes, use DbSet directly with IgnoreQueryFilters
        if (includes == null || !includes.Any())
        {
            var entity = await _context.Set<TEntity>().FindAsync(id);
            if (entity != null)
            {
                // Manually check if it's deleted
                if (entity is BaseEntity baseEntity && baseEntity.IsDeleted == true)
                {
                    // If it's deleted, return it anyway since we're including deleted
                    _context.Entry(entity).State = EntityState.Detached;
                    return entity;
                }
                else if (!(entity is BaseEntity))
                {
                    // If it's not a BaseEntity, return it
                    _context.Entry(entity).State = EntityState.Detached;
                    return entity;
                }
            }

            // If we get here, either entity is null or it's a BaseEntity but not deleted
            // In this case, try to find it with IgnoreQueryFilters
            var keyProperty = _context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties[0];
            if (keyProperty == null)
                throw new InvalidOperationException("Entity does not have a primary key");

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(parameter, keyProperty.Name);
            var constant = Expression.Constant(id);
            var equality = Expression.Equal(property, Expression.Convert(constant, keyProperty.ClrType));
            var lambda = Expression.Lambda<Func<TEntity, bool>>(equality, parameter);

            return await _context.Set<TEntity>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(lambda);
        }

        // Build the query with includes
        var keyProperty2 = _context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties[0];
        if (keyProperty2 == null)
            throw new InvalidOperationException("Entity does not have a primary key");

        var parameter2 = Expression.Parameter(typeof(TEntity), "e");
        var property2 = Expression.Property(parameter2, keyProperty2.Name);
        var constant2 = Expression.Constant(id);
        var equality2 = Expression.Equal(property2, Expression.Convert(constant2, keyProperty2.ClrType));
        var lambda2 = Expression.Lambda<Func<TEntity, bool>>(equality2, parameter2);

        var query = _context.Set<TEntity>()
            .IgnoreQueryFilters()
            .AsNoTracking();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

        return await query.FirstOrDefaultAsync(lambda2);
    }

    public virtual async Task<TEntity> SoftDeleteAsync(TEntity entity, string userId)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity is BaseEntity baseEntity)
        {
            baseEntity.SoftDelete(userId);
            return await UpdateAsync(entity);
        }

        throw new InvalidOperationException("Entity does not inherit from BaseEntity");
    }

    public virtual async Task<TEntity> RestoreAsync(TEntity entity, string userId)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity is BaseEntity baseEntity)
        {
            baseEntity.Restore(userId);
            return await UpdateAsync(entity);
        }

        throw new InvalidOperationException("Entity does not inherit from BaseEntity");
    }

    // AnyAsync
    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    // FindAsync with includes
    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        return await query.Where(predicate).ToListAsync();
    }

    // FindAsync with ordering and includes
    public virtual async Task<IEnumerable<TEntity>> FindAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        query = query.Where(predicate);

        return ascending
            ? await query.OrderBy(orderBy).ToListAsync()
            : await query.OrderByDescending(orderBy).ToListAsync();
    }

    // GetPagedAsync with ordering and filtering
    public virtual async Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync<TKey>(
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool ascending,
        int pageNumber,
        int pageSize,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // Start with all entities
        var query = _dbSet.AsNoTracking();

        // Apply includes
        if (includes?.Any() == true)
        {
            query = includes.Aggregate(query,
                (current, include) => current.Include(include));
        }

        // Apply filter
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Optimized counting strategy
        int totalCount;

        // Apply ordering first, then do the counting with Take
        var orderedQuery = ascending
            ? query.OrderBy(orderBy)
            : query.OrderByDescending(orderBy);

        // First try a faster count by limiting the query
        // Only count up to a reasonable threshold (e.g., 10 pages worth)
        const int countThreshold = 1000;
        var quickCountQuery = orderedQuery.Select(e => 1).Take(countThreshold);
        var quickCount = await quickCountQuery.CountAsync();

        // If count is less than the threshold, we already have the exact count
        // Otherwise, we need to do a full count if we need the exact total
        if (quickCount < countThreshold)
        {
            totalCount = quickCount;
        }
        else
        {
            // We've hit the threshold, so the count is at least the threshold
            // Now we need to decide if we need an exact count

            // Option 1: Return approximate count (faster)
            // totalCount = countThreshold; // Just return "1000+" records

            // Option 2: Get exact count (slower but accurate)
            totalCount = await query.CountAsync();
        }

        // Apply pagination - use the already ordered query
        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

}

