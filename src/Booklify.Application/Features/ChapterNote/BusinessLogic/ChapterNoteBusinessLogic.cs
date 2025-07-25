using AutoMapper;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.ChapterNote.BusinessLogic;

/// <summary>
/// Business logic implementation for chapter note operations
/// Follows clean architecture principles with method parameters instead of injected dependencies
/// </summary>
public class ChapterNoteBusinessLogic : IChapterNoteBusinessLogic
{
    /// <summary>
    /// Validate user authentication and get user profile information
    /// </summary>
    public async Task<Result<UserProfile>> ValidateUserAndGetProfileAsync(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        var isUserValid = await currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<UserProfile>.Failure("User Unauthorized", ErrorCode.Unauthorized);
        }

        var currentUserId = currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Result<UserProfile>.Failure("User Unauthorized", ErrorCode.Unauthorized);
        }

        var userProfile = await unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(
            u => u.IdentityUserId == currentUserId);

        if (userProfile == null)
        {
            return Result<UserProfile>.Failure("User Not Found", ErrorCode.NotFound);
        }

        return Result<UserProfile>.Success(userProfile);
    }

    /// <summary>
    /// Validate chapter exists and user has access
    /// </summary>
    public async Task<Result<Domain.Entities.Chapter>> ValidateChapterAccessAsync(
        Guid chapterId,
        IUnitOfWork unitOfWork)
    {
        var chapter = await unitOfWork.ChapterRepository.GetByIdAsync(
            chapterId,
            c => c.Book!);

        if (chapter == null)
        {
            return Result<Domain.Entities.Chapter>.Failure("Chapter Not Found", ErrorCode.NotFound);
        }

        // Add additional access validation if needed
        // For example, check if book is published, user has subscription for premium books, etc.
        if (chapter.Status != EntityStatus.Active)
        {
            return Result<Domain.Entities.Chapter>.Failure("Chapter is not available", ErrorCode.ValidationFailed);
        }

        return Result<Domain.Entities.Chapter>.Success(chapter);
    }

    /// <summary>
    /// Create chapter note entity with business rules
    /// </summary>
    public async Task<Result<Domain.Entities.ChapterNote>> CreateChapterNoteEntityAsync(
        CreateChapterNoteRequest request,
        UserProfile userProfile,
        string userId,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        // Business rule: Check if user already has too many notes for this chapter
        var existingNotesCount = await unitOfWork.ChapterNoteRepository.CountAsync(
            n => n.UserId == userProfile.Id && 
                 n.ChapterId == request.ChapterId && 
                 n.Status == EntityStatus.Active);

        if (existingNotesCount >= 100) // Max 100 notes per chapter per user
        {
            return Result<Domain.Entities.ChapterNote>.Failure(
                "You have reached the maximum limit of 100 notes for this chapter", 
                ErrorCode.ValidationFailed);
        }

        // Business rule: Validate note type and required fields
        if (request.NoteType == ChapterNoteType.Highlight)
        {
            if (string.IsNullOrEmpty(request.HighlightedText))
            {
                return Result<Domain.Entities.ChapterNote>.Failure(
                    "Highlight note type must have highlighted text", 
                    ErrorCode.ValidationFailed);
            }
        }
        else if (request.NoteType == ChapterNoteType.TextNote)
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return Result<Domain.Entities.ChapterNote>.Failure(
                    "Text note type must have content", 
                    ErrorCode.ValidationFailed);
            }
        }

        // Map request to entity
        var note = mapper.Map<Domain.Entities.ChapterNote>(request);
        // Đảm bảo set lại 2 trường này (dù đã mapping)
        note.CfiStart = request.CfiStart;
        note.CfiEnd = request.CfiEnd;
        
        // Set additional properties not in request
        note.UserId = userProfile.Id;
        note.Status = EntityStatus.Active;
        
        // Initialize base entity
        BaseEntityExtensions.InitializeBaseEntity(note, userId);
        await unitOfWork.ChapterNoteRepository.AddAsync(note);
        
        return Result<Domain.Entities.ChapterNote>.Success(note);
    }

    /// <summary>
    /// Get chapter note by ID with ownership validation
    /// </summary>
    public async Task<Result<ChapterNoteResponse>> GetChapterNoteByIdAsync(
        Guid noteId,
        Guid userId,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        var note = await unitOfWork.ChapterNoteRepository.GetByIdAsync(
            noteId,
            n => n.Chapter!,
            n => n.Chapter!.Book!);

        if (note == null)
        {
            return Result<ChapterNoteResponse>.Failure("Note not found", ErrorCode.NotFound);
        }

        // Validate ownership
        if (note.UserId != userId)
        {
            return Result<ChapterNoteResponse>.Failure("You do not have permission to access this note", ErrorCode.Forbidden);
        }

        // Validate status
        if (note.Status != EntityStatus.Active)
        {
            return Result<ChapterNoteResponse>.Failure("Note is not available", ErrorCode.ValidationFailed);
        }

        var response = mapper.Map<ChapterNoteResponse>(note);
        return Result<ChapterNoteResponse>.Success(response);
    }

    /// <summary>
    /// Update chapter note with business rules validation
    /// </summary>
    public async Task<Result<Domain.Entities.ChapterNote>> UpdateChapterNoteAsync(
        Guid noteId,
        UpdateChapterNoteRequest request,
        Guid userId,
        string currentUserId,
        IUnitOfWork unitOfWork)
    {
        var note = await unitOfWork.ChapterNoteRepository.GetByIdAsync(
            noteId,
            n => n.Chapter!,
            n => n.Chapter!.Book!);

        if (note == null)
        {
            return Result<Domain.Entities.ChapterNote>.Failure("Note not found", ErrorCode.NotFound);
        }

        // Validate ownership - here we use userId from UserProfile
        if (note.UserId != userId)
        {
            return Result<Domain.Entities.ChapterNote>.Failure("You do not have permission to update this note", ErrorCode.Forbidden);
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Content))
        {
            note.Content = request.Content;
        }

        if (request.PageNumber.HasValue)
        {
            note.PageNumber = request.PageNumber.Value;
        }

        if (request.Cfi != null)
        {
            note.Cfi = request.Cfi;
        }

        if (request.HighlightedText != null)
        {
            note.HighlightedText = request.HighlightedText;
        }

        if (request.Color != null)
        {
            note.Color = request.Color;
        }

        if (request.NoteType.HasValue)
        {
            note.NoteType = request.NoteType.Value;
        }

        if (request.CfiStart != null)
        {
            note.CfiStart = request.CfiStart;
        }

        if (request.CfiEnd != null)
        {
            note.CfiEnd = request.CfiEnd;
        }

        // Update audit info with current identity user ID
        note.UpdateBaseEntity(currentUserId);
        
        // Update the entity in repository
        await unitOfWork.ChapterNoteRepository.UpdateAsync(note);

        return Result<Domain.Entities.ChapterNote>.Success(note);
    }

    /// <summary>
    /// Delete chapter note with ownership validation
    /// </summary>
    public async Task<Result<bool>> DeleteChapterNoteAsync(
        Guid noteId,
        Guid userId,
        string currentUserId,
        IUnitOfWork unitOfWork)
    {
        var note = await unitOfWork.ChapterNoteRepository.GetByIdAsync(noteId);

        if (note == null)
        {
            return Result<bool>.Failure("Note not found", ErrorCode.NotFound);
        }

        // Validate ownership
        if (note.UserId != userId)
        {
            return Result<bool>.Failure("You do not have permission to delete this note", ErrorCode.Forbidden);
        }

        // Soft delete
        await unitOfWork.ChapterNoteRepository.SoftDeleteAsync(note, currentUserId);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Get paged chapter notes for current user with filters
    /// </summary>
    public async Task<Result<PaginatedResult<ChapterNoteListItemResponse>>> GetPagedChapterNotesAsync(
        ChapterNoteFilterModel filter,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        // Validate user and get profile
        var userResult = await ValidateUserAndGetProfileAsync(currentUserService, unitOfWork);
        if (!userResult.IsSuccess)
        {
            return Result<PaginatedResult<ChapterNoteListItemResponse>>.Failure(
                userResult.Message,
                ErrorCode.Unauthorized);
        }

        var userProfile = userResult.Data;
        
        // Set user filter to only get notes of current user
        var filterToUse = filter ?? new ChapterNoteFilterModel();
        filterToUse.UserId = userProfile.Id;
        filterToUse.Status = EntityStatus.Active; // Only get active notes
        
        // Get paged notes from repository
        var (notes, totalCount) = await unitOfWork.ChapterNoteRepository.GetPagedChapterNotesAsync(filterToUse);
        
        // Map to response DTOs using AutoMapper
        var noteResponses = mapper.Map<List<ChapterNoteListItemResponse>>(notes);
        
        // Return paginated result
        var result = PaginatedResult<ChapterNoteListItemResponse>.Success(
            noteResponses, 
            filterToUse.PageNumber,
            filterToUse.PageSize,
            totalCount);
            
        return Result<PaginatedResult<ChapterNoteListItemResponse>>.Success(result);
    }
}
