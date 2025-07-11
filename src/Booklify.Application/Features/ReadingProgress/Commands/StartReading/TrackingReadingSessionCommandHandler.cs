using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Booklify.Application.Common.DTOs.ReadingProgress;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.ReadingProgress.Commands.StartReading;

public class TrackingReadingSessionCommandHandler : IRequestHandler<TrackingReadingSessionCommand, Result<TrackingSessionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TrackingReadingSessionCommandHandler> _logger;
    
    public TrackingReadingSessionCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<TrackingReadingSessionCommandHandler> logger
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<TrackingSessionResponse>> Handle(TrackingReadingSessionCommand command, CancellationToken cancellationToken)
    {
        try
        {
            //1. validate user
            var isUserValid = await _currentUserService.IsUserValidAsync();
            if (!isUserValid)
            {
                return Result<TrackingSessionResponse>.Failure("User is not authorized", ErrorCode.Unauthorized);
            }

            var currentUserId = _currentUserService.UserId;

            var userProfile = await _unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(x => x.IdentityUserId == currentUserId);
            if (userProfile == null)
            {
                return Result<TrackingSessionResponse>.Failure("User profile not found", ErrorCode.NotFound);
            }
            
            //2. Get book info for TotalChapters calculation
            var book = await _unitOfWork.BookRepository.GetFirstOrDefaultAsync(x => x.Id == command.Request.BookId);
            if (book == null)
            {
                return Result<TrackingSessionResponse>.Failure("Book not found", ErrorCode.NotFound);
            }
            
            //3. map request to reading progress
            Domain.Entities.ReadingProgress? readingProgress = null;
            bool isNewReadingProgress = false;
            //begin transaction
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                //4. validate if reading progress exists
                var existingReadingProgress = await _unitOfWork.ReadingProgressRepository.GetFirstOrDefaultAsync(
                    x => x.UserId == userProfile.Id && x.BookId == command.Request.BookId);
                    
                if (existingReadingProgress == null)
                {
                    isNewReadingProgress = true;
                    //reading progress not exists, create new one
                    readingProgress = _mapper.Map<Domain.Entities.ReadingProgress>(command.Request);
                    await HandleStartReadingProgress(readingProgress, userProfile.Id, currentUserId, command.Request.ChapterId, command.Request.IsCompleted);
                }
                else
                {
                    //reading progress exists, update existing one
                    readingProgress = existingReadingProgress;
                    await HandleUpdateReadingProgress(readingProgress, currentUserId, command.Request.ChapterId);
                }
                
                //5. track chapter reading progress
                ChapterReadingProgress? chapterReadingProgress = null;
                string operationMessage;
                
                if (isNewReadingProgress) 
                {
                    // New reading progress = new chapter reading progress (guaranteed)
                    chapterReadingProgress = _mapper.Map<ChapterReadingProgress>(command.Request);
                    await HandleCreateChapterReadingProgress(readingProgress, chapterReadingProgress, currentUserId, isNewReadingProgress: true);
                    operationMessage = "Reading progress tracked successfully";
                }
                else
                {
                    // Existing reading progress - check if chapter reading progress exists
                    chapterReadingProgress = await _unitOfWork.ChapterReadingProgressRepository.GetFirstOrDefaultAsync(
                        x => x.ReadingProgressId == readingProgress.Id && x.ChapterId == command.Request.ChapterId);
                        
                    if (chapterReadingProgress == null)
                    {
                        // Chapter reading progress doesn't exist, create new one
                        chapterReadingProgress = _mapper.Map<ChapterReadingProgress>(command.Request);
                        await HandleCreateChapterReadingProgress(readingProgress, chapterReadingProgress, currentUserId, isNewReadingProgress: false);
                        operationMessage = "Reading progress tracked successfully";
                    }
                    else
                    {
                        // Chapter reading progress exists, update it
                        var wasAlreadyCompleted = chapterReadingProgress.IsCompleted;
                        await HandleUpdateChapterReadingProgress(chapterReadingProgress, readingProgress, currentUserId, command.Request);
                        operationMessage = wasAlreadyCompleted 
                            ? "Reading progress updated successfully (chapter already completed)"
                            : "Reading progress updated successfully";
                    }
                }

                // Update ReadingProgress timestamps only for existing entities
                if (!isNewReadingProgress)
                {
                    // ✅ Only update existing entities - new ones already have correct timestamps from creation
                    await UpdateEntityWithTimestamp(readingProgress, currentUserId);
                }
                // ✅ New entities: No update needed! All timestamps were set correctly during creation

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                //6. return tracking response - no query reload needed
                var response = _mapper.Map<TrackingSessionResponse>(readingProgress);
                
                // Set fields manually from available data
                response.TotalChaptersCount = book.TotalChapters;
                response.OverallProgressPercentage = book.TotalChapters > 0 
                    ? Math.Round((double)readingProgress.CompletedChaptersCount / book.TotalChapters * 100, 2) 
                    : 0;
                
                return Result<TrackingSessionResponse>.Success(response, operationMessage);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking reading progress");
            return Result<TrackingSessionResponse>.Failure("Error tracking reading progress", ErrorCode.InternalError);
        }
    }

    private async Task HandleStartReadingProgress(Domain.Entities.ReadingProgress readingProgress, Guid userProfileId, string currentUserId, Guid chapterId, bool isChapterCompleted)
    {
        readingProgress.CurrentChapterId = chapterId;
        readingProgress.UserId = userProfileId;
        
        // ✅ Set completion count immediately if first chapter is completed
        // This prevents EF from thinking the entity was modified after creation
        readingProgress.CompletedChaptersCount = isChapterCompleted ? 1 : 0;
        
        BaseEntityExtensions.InitializeBaseEntity(readingProgress, currentUserId);
        await _unitOfWork.ReadingProgressRepository.AddAsync(readingProgress);
    }

    private async Task HandleUpdateReadingProgress(Domain.Entities.ReadingProgress readingProgress, string currentUserId, Guid chapterId)
    {
        // Update current chapter if different
        if (readingProgress.CurrentChapterId != chapterId)
        {
            readingProgress.CurrentChapterId = chapterId;
        }
        
        // LastReadAt will be updated in UpdateEntityWithTimestamp at the end
    }

    private async Task HandleCreateChapterReadingProgress(Domain.Entities.ReadingProgress readingProgress, ChapterReadingProgress chapterReadingProgress, string currentUserId, bool isNewReadingProgress = false)
    {
        // ✅ Use navigation property instead of foreign key ID to avoid FK constraint issues
        chapterReadingProgress.ReadingProgress = readingProgress;
        // Entity Framework will automatically set ReadingProgressId when SaveChanges is called
        
        // Set timestamps for new tracking record
        chapterReadingProgress.LastReadAt = DateTime.UtcNow;
        BaseEntityExtensions.InitializeBaseEntity(chapterReadingProgress, currentUserId);
        await _unitOfWork.ChapterReadingProgressRepository.AddAsync(chapterReadingProgress);
        
        // ✅ Only increment count for existing reading progress
        // For new reading progress, the count was already set correctly in HandleStartReadingProgress
        if (!isNewReadingProgress && chapterReadingProgress.IsCompleted)
        {
            IncrementCompletionCount(readingProgress);
        }
    }

    private async Task HandleUpdateChapterReadingProgress(ChapterReadingProgress chapterReadingProgress, Domain.Entities.ReadingProgress readingProgress, string currentUserId, TrackingReadingSessionRequest request)
    {
        var wasCompletedBefore = chapterReadingProgress.IsCompleted;
        
        // Update CFI position if provided
        if (!string.IsNullOrEmpty(request.CurrentCfi) && chapterReadingProgress.CurrentCfi != request.CurrentCfi)
        {
            chapterReadingProgress.CurrentCfi = request.CurrentCfi;
        }
        
        // Chapter completion is IMMUTABLE: once completed, cannot be reverted
        if (!chapterReadingProgress.IsCompleted && request.IsCompleted)
        {
            chapterReadingProgress.IsCompleted = true;
            chapterReadingProgress.CompletedAt = DateTime.UtcNow;
        }
        
        // Update timestamps and save tracking record
        await UpdateChapterEntityWithTimestamp(chapterReadingProgress, currentUserId);
        
        // Update reading progress if chapter was just completed (not if already completed)
        if (!wasCompletedBefore && chapterReadingProgress.IsCompleted)
        {
            IncrementCompletionCount(readingProgress);
        }
    }
    
    private void IncrementCompletionCount(Domain.Entities.ReadingProgress readingProgress)
    {
        readingProgress.CompletedChaptersCount++;
        
        // Check if book is now fully completed
        if (readingProgress.Book != null && 
            readingProgress.CompletedChaptersCount >= readingProgress.Book.TotalChapters && 
            !readingProgress.IsCompleted)
        {
            readingProgress.IsCompleted = true;
        }
    }
    
    private async Task UpdateEntityWithTimestamp(Domain.Entities.ReadingProgress entity, string currentUserId)
    {
        entity.LastReadAt = DateTime.UtcNow;
        entity.UpdateBaseEntity(currentUserId);
        await _unitOfWork.ReadingProgressRepository.UpdateAsync(entity);
    }
    
    private async Task UpdateChapterEntityWithTimestamp(ChapterReadingProgress entity, string currentUserId)
    {
        entity.LastReadAt = DateTime.UtcNow;
        entity.UpdateBaseEntity(currentUserId);
        await _unitOfWork.ChapterReadingProgressRepository.UpdateAsync(entity);
    }
}