using System.Data;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface for coordinating multiple database contexts
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// The main business context
    /// </summary>
    IBooklifyDbContext BusinessContext { get; }

    /// <summary>
    /// Begin a transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Begin a transaction with specified isolation level
    /// </summary>
    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save changes on business context
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
} 