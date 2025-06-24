using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Interfaces.Repositories;

namespace Booklify.Infrastructure.Persistence;

/// <summary>
/// Implementation of the Unit of Work pattern for coordinating multiple database contexts
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly BooklifyDbContext _businessContext;
    private IDbContextTransaction? _transaction;
    private IStaffProfileRepository _staffProfileRepository;
    private IBookCategoryRepository _bookCategoryRepository;
    private IBookRepository _bookRepository;
    private IChapterRepository _chapterRepository;
    private IUserProfileRepository _userProfileRepository;
    private IFileInfoRepository _fileInfoRepository;

    public UnitOfWork(
        BooklifyDbContext businessContext,
        IStaffProfileRepository staffProfileRepository,
        IBookCategoryRepository bookCategoryRepository,
        IBookRepository bookRepository,
        IChapterRepository chapterRepository,
        IUserProfileRepository userProfileRepository,
        IFileInfoRepository fileInfoRepository)
    {
        _businessContext = businessContext;
        _staffProfileRepository = staffProfileRepository;
        _bookCategoryRepository = bookCategoryRepository;
        _bookRepository = bookRepository;
        _chapterRepository = chapterRepository;
        _userProfileRepository = userProfileRepository;
        _fileInfoRepository = fileInfoRepository;
    }

    public IBooklifyDbContext BusinessContext => _businessContext;

    public IStaffProfileRepository StaffProfileRepository => _staffProfileRepository;

    public IBookCategoryRepository BookCategoryRepository => _bookCategoryRepository;

    public IBookRepository BookRepository => _bookRepository;

    public IChapterRepository ChapterRepository => _chapterRepository;

    public IUserProfileRepository UserProfileRepository => _userProfileRepository;

    public IFileInfoRepository FileInfoRepository => _fileInfoRepository;

    /// <summary>
    /// Begin a transaction
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _businessContext.Database.BeginTransactionAsync(cancellationToken);
    }
    
    /// <summary>
    /// Begin a transaction with specified isolation level
    /// </summary>
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        _transaction = await _businessContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }
    
    /// <summary>
    /// Commit the current transaction
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
    
    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    /// <summary>
    /// Save changes to business context
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _businessContext.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Dispose the unit of work and transaction
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
} 