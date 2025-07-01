using System.Data;
using Booklify.Application.Common.Interfaces.Repositories;

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
    /// The staff profile repository
    /// </summary>
    IStaffProfileRepository StaffProfileRepository { get; }

    /// <summary>
    /// The book category repository
    /// </summary>
    IBookCategoryRepository BookCategoryRepository { get; }

    /// <summary>
    /// The book repository
    /// </summary>
    IBookRepository BookRepository { get; }

    /// <summary>
    /// The chapter repository
    /// </summary>
    IChapterRepository ChapterRepository { get; }

    /// <summary>
    /// The user profile repository
    /// </summary>
    IUserProfileRepository UserProfileRepository { get; }

    /// <summary>
    /// The file info repository
    /// </summary>
    IFileInfoRepository FileInfoRepository { get; }

    /// <summary>
    /// The chapter AI result repository
    /// </summary>
    IChapterAIResultRepository ChapterAIResultRepository { get; }

    /// <summary>
    /// The chapter note repository
    /// </summary>
    IChapterNoteRepository ChapterNoteRepository { get; }

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