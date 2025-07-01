using AutoMapper;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;

namespace Booklify.Application.Features.ChapterNote.BusinessLogic;

/// <summary>
/// Interface for chapter note business logic operations
/// </summary>
public interface IChapterNoteBusinessLogic
{
    /// <summary>
    /// Validate user authentication and get user profile information
    /// </summary>
    Task<Result<UserProfile>> ValidateUserAndGetProfileAsync(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Validate chapter exists and user has access
    /// </summary>
    Task<Result<Domain.Entities.Chapter>> ValidateChapterAccessAsync(
        Guid chapterId,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Create chapter note entity with business rules
    /// </summary>
    Task<Result<Domain.Entities.ChapterNote>> CreateChapterNoteEntityAsync(
        CreateChapterNoteRequest request,
        UserProfile userProfile,
        string userId,
        IUnitOfWork unitOfWork,
        IMapper mapper);

    /// <summary>
    /// Get chapter note by ID with ownership validation
    /// </summary>
    Task<Result<ChapterNoteResponse>> GetChapterNoteByIdAsync(
        Guid noteId,
        Guid userId,
        IUnitOfWork unitOfWork,
        IMapper mapper);

    /// <summary>
    /// Update chapter note with business rules validation
    /// </summary>
    Task<Result<Domain.Entities.ChapterNote>> UpdateChapterNoteAsync(
        Guid noteId,
        UpdateChapterNoteRequest request,
        Guid userId,
        string currentUserId,
        IUnitOfWork unitOfWork);

    /// <summary>
    /// Delete chapter note with ownership validation
    /// </summary>
    Task<Result<bool>> DeleteChapterNoteAsync(
        Guid noteId,
        Guid userId,
        string currentUserId,
        IUnitOfWork unitOfWork);

    Task<Result<PaginatedResult<ChapterNoteListItemResponse>>> GetPagedChapterNotesAsync(
        ChapterNoteFilterModel filter,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper);
}
