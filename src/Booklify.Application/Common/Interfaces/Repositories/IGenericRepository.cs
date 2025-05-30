using System.Linq.Expressions;

namespace Booklify.Application.Common.Interfaces.Repositories;

/// <summary>
/// Generic repository interface for CRUD operations
/// </summary>
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllAsync<TKey>(Expression<Func<TEntity, TKey>> orderBy, bool ascending = true);
    Task<IEnumerable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes);
    Task<IEnumerable<TEntity>> GetAllAsync<TKey>(
        Expression<Func<TEntity, TKey>> orderBy, 
        bool ascending = true, 
        params Expression<Func<TEntity, object>>[] includes);
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes);
    Task<TEntity> AddAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression);
    IQueryable<TEntity> FindByCondition(
        Expression<Func<TEntity, bool>> expression, 
        params Expression<Func<TEntity, object>>[] includes);
    IQueryable<TEntity> FindByCondition<TKey>(
        Expression<Func<TEntity, bool>> expression, 
        Expression<Func<TEntity, TKey>> orderBy, 
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes);
    Task<TEntity?> GetByIdAsync(object id, params Expression<Func<TEntity, object>>[] includes);
    
    // Range operations
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, bool fromNoTracking);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, bool fromNoTracking);
    
    // Bulk soft delete operations
    Task<IEnumerable<TEntity>> SoftDeleteRangeAsync(IEnumerable<TEntity> entities, string userId);
    Task<int> SoftDeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, string userId);
    Task<IEnumerable<TEntity>> RestoreRangeAsync(IEnumerable<TEntity> entities, string userId);
    
    // Query operations
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, 
        params Expression<Func<TEntity, object>>[] includes);
    Task<IEnumerable<TEntity>> FindAsync<TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes);
    Task<IEnumerable<TEntity>> FindAsyncForUpdate(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes);
    Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync<TKey>(
        Expression<Func<TEntity, bool>>? predicate, 
        Expression<Func<TEntity, TKey>> orderBy,
        bool ascending,
        int pageNumber,
        int pageSize,
        params Expression<Func<TEntity, object>>[] includes);
    
    // Soft delete operations
    Task<TEntity> SoftDeleteAsync(TEntity entity, string userId);
    Task<TEntity> RestoreAsync(TEntity entity, string userId);
    
    // Include deleted operations
    IQueryable<TEntity> FindByConditionIncludeDeleted(Expression<Func<TEntity, bool>> expression);
    Task<IEnumerable<TEntity>> GetAllIncludeDeletedAsync();
    Task<TEntity?> GetByIdIncludeDeletedAsync(Guid id, params Expression<Func<TEntity, object>>[] includes);
}