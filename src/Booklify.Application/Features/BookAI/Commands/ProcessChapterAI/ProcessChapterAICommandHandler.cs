using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookAI;
using Booklify.Domain.Entities;
using Booklify.Domain.Commons;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.BookAI.Commands.ProcessChapterAI;

public class ProcessChapterAICommandHandler : IRequestHandler<ProcessChapterAICommand, Result<ChapterAIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITextAIService _aiService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProcessChapterAICommandHandler> _logger;

    public ProcessChapterAICommandHandler(
        IUnitOfWork unitOfWork,
        ITextAIService aiService,
        ICurrentUserService currentUserService,
        ILogger<ProcessChapterAICommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ChapterAIResponse>> Handle(ProcessChapterAICommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = _currentUserService.UserId ?? string.Empty;
        
        try
        {
            // 0. Check if user is subscribed
            var userProfile = await _unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (userProfile == null)
            {
                return Result<ChapterAIResponse>.Failure("User not found", ErrorCode.Unauthorized);
            }

            var isSubscribed = await _unitOfWork.UserSubscriptionRepository.AnyAsync(c => c.UserId == userProfile.Id && 
            c.Status == EntityStatus.Active && c.EndDate > DateTime.UtcNow);

            if (!isSubscribed)
            {
                return Result<ChapterAIResponse>.Failure("User is not subscribed", ErrorCode.ValidationFailed);
            }

            // 1. Get book and validate
            var book = await _unitOfWork.BookRepository.GetFirstOrDefaultAsync(
                b => b.Id == request.BookId && !b.IsDeleted
            );

            if (book == null)
            {
                return Result<ChapterAIResponse>.Failure("Book not found", ErrorCode.NotFound);
            }

            // 2. Get chapter by ID and validate
            var chapter = await _unitOfWork.ChapterRepository.GetFirstOrDefaultAsync(
                c => c.Id == request.ChapterId && c.BookId == request.BookId && !c.IsDeleted
            );

            if (chapter == null)
            {
                return Result<ChapterAIResponse>.Failure("Chapter not found", ErrorCode.NotFound);
            }

            // 3. Use content from client
            var content = request.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<ChapterAIResponse>.Failure("Chapter content is required", ErrorCode.InvalidInput);
            }

            // 4. Process AI actions
            var response = new ChapterAIResponse
            {
                ChapterTitle = chapter.Title
            };

            var processedActions = new List<string>();

            _logger.LogInformation("Processing {ActionCount} actions: {Actions} for chapter {ChapterId}", 
                request.Actions.Count, string.Join(", ", request.Actions), request.ChapterId);

            foreach (var action in request.Actions.Select(a => a.ToLower()).Distinct())
            {
                try
                {
                    _logger.LogInformation("Processing action: {Action}", action);
                    switch (action)
                    {
                        case "summary":
                            _logger.LogInformation("Calling AI service for summary...");
                            var summary = await _aiService.SummarizeAsync(content);
                            _logger.LogInformation("Summary result length: {Length}", summary?.Length ?? 0);
                            if (!string.IsNullOrEmpty(summary))
                            {
                                response.Summary = summary;
                                processedActions.Add("summary");
                                _logger.LogInformation("Summary added to processed actions");
                            }
                            else
                            {
                                _logger.LogWarning("Summary is null or empty");
                            }
                            break;

                        case "keywords":
                            var keywords = await _aiService.ExtractKeywordsAsync(content);
                            if (keywords != null && keywords.Any())
                            {
                                response.Keywords = keywords;
                                processedActions.Add("keywords");
                            }
                            break;

                        case "translation":
                            var translation = await _aiService.TranslateAsync(content);
                            if (!string.IsNullOrEmpty(translation))
                            {
                                response.Translation = translation;
                                processedActions.Add("translation");
                            }
                            break;

                        case "flashcards":
                            var flashcards = await _aiService.GenerateFlashcardsAsync(content);
                            if (flashcards != null && flashcards.Any())
                            {
                                response.Flashcards = flashcards;
                                processedActions.Add("flashcards");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing action {Action} for chapter {ChapterId}", action, chapter.Id);
                }
            }

            response.ProcessedActions = processedActions;

            // 5. Save result to database (optional, for caching)
            if (processedActions.Any())
            {
                await SaveChapterAIResult(chapter.Id, response, processedActions, userId);
            }

            stopwatch.Stop();
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Processed AI actions {Actions} for chapter {ChapterId} in {ElapsedMs}ms", 
                string.Join(", ", processedActions), chapter.Id, stopwatch.ElapsedMilliseconds);

            return Result<ChapterAIResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI for chapter {ChapterId} in book {BookId}", request.ChapterId, request.BookId);
            return Result<ChapterAIResponse>.Failure("Failed to process AI for chapter", ErrorCode.InternalError);
        }
    }

    private async Task SaveChapterAIResult(Guid chapterId, ChapterAIResponse response, List<string> processedActions, string userId)
    {
        try
        {
            // Check if result already exists
            var existingResult = await _unitOfWork.ChapterAIResultRepository.GetFirstOrDefaultAsync(
                r => r.ChapterId == chapterId && !r.IsDeleted);

            var chapterAIResult = existingResult ?? new ChapterAIResult { ChapterId = chapterId };

            // Update fields based on processed actions
            if (processedActions.Contains("summary") && !string.IsNullOrEmpty(response.Summary))
            {
                chapterAIResult.Summary = response.Summary;
            }
            
            if (processedActions.Contains("translation") && !string.IsNullOrEmpty(response.Translation))
            {
                chapterAIResult.Translation = response.Translation;
            }
            
            if (processedActions.Contains("keywords") && response.Keywords != null)
            {
                chapterAIResult.Keywords = JsonSerializer.Serialize(response.Keywords);
            }
            
            if (processedActions.Contains("flashcards") && response.Flashcards != null)
            {
                chapterAIResult.Flashcards = JsonSerializer.Serialize(response.Flashcards);
            }

            chapterAIResult.ProcessedActions = string.Join(",", processedActions);
            chapterAIResult.AIModel = "gemini-1.5-flash";

            if (existingResult == null)
            {
                BaseEntityExtensions.InitializeBaseEntity(chapterAIResult, userId);
                await _unitOfWork.ChapterAIResultRepository.AddAsync(chapterAIResult);
            }
            else
            {
                chapterAIResult.ModifiedAt = DateTime.UtcNow;
                chapterAIResult.ModifiedBy = Guid.TryParse(userId, out var userGuid) ? userGuid : null;
                await _unitOfWork.ChapterAIResultRepository.UpdateAsync(chapterAIResult);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving AI result for chapter {ChapterId}", chapterId);
            // Don't throw - this is just for caching
        }
    }
} 