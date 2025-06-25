using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.BookAI;
using Booklify.Domain.Entities;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.BookAI.Commands.ProcessChapterAI;

public class ProcessChapterAICommandHandler : IRequestHandler<ProcessChapterAICommand, Result<ChapterAIResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly ITextAIService _aiService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProcessChapterAICommandHandler> _logger;

    public ProcessChapterAICommandHandler(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        ITextAIService aiService,
        ICurrentUserService currentUserService,
        ILogger<ProcessChapterAICommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
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
            // 1. Get book and validate
            var book = await _unitOfWork.BookRepository.GetFirstOrDefaultAsync(
                b => b.Id == request.BookId && !b.IsDeleted,
                b => b.File,
                b => b.Chapters
            );

            if (book == null)
            {
                return Result<ChapterAIResponse>.Failure("Book not found", ErrorCode.NotFound);
            }

            if (book.File == null)
            {
                return Result<ChapterAIResponse>.Failure("Book file not found", ErrorCode.NotFound);
            }

            // 2. Get chapter by index
            var chapters = book.Chapters?
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Order)
                .ToList();

            if (chapters == null || !chapters.Any())
            {
                return Result<ChapterAIResponse>.Failure("No chapters found for this book", ErrorCode.NotFound);
            }

            if (request.ChapterIndex >= chapters.Count)
            {
                return Result<ChapterAIResponse>.Failure($"Chapter index {request.ChapterIndex} is out of range. Book has {chapters.Count} chapters.", ErrorCode.InvalidInput);
            }

            var chapter = chapters[request.ChapterIndex];

            // 3. Extract chapter content
            var content = await ExtractChapterContent(book, chapter);
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<ChapterAIResponse>.Failure("Could not extract chapter content", ErrorCode.InvalidInput);
            }

            // 4. Process AI actions
            var response = new ChapterAIResponse
            {
                ChapterTitle = chapter.Title
            };

            var processedActions = new List<string>();

            _logger.LogInformation("Processing {ActionCount} actions: {Actions}", request.Actions.Count, string.Join(", ", request.Actions));

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
            _logger.LogError(ex, "Error processing AI for chapter {ChapterIndex} in book {BookId}", request.ChapterIndex, request.BookId);
            return Result<ChapterAIResponse>.Failure("Failed to process AI for chapter", ErrorCode.InternalError);
        }
    }

    private async Task<string> ExtractChapterContent(Domain.Entities.Book book, Chapter chapter)
    {
        try
        {
            // Download EPUB file
            var fileStream = await _storageService.DownloadFileAsync(book.File!.FilePath);
            if (fileStream == null)
            {
                return string.Empty;
            }

            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            var tempFilePath = await CreateTempFileFromContent(fileContent, book.File.Extension);
            try
            {
                // For now, return sample content since we removed VersOne.Epub dependency
                // In a real implementation, you would use an EPUB reader library
                var sampleContent = $"Chapter content for: {chapter.Title}\n\nThis is sample content that would be extracted from the EPUB file. In a real implementation, you would use a library like VersOne.Epub to extract the actual chapter content from the EPUB file.";
                
                // Limit content length for API calls
                const int maxContentLength = 4000;
                if (sampleContent.Length > maxContentLength)
                {
                    sampleContent = sampleContent.Substring(0, maxContentLength) + "...";
                }
                
                return sampleContent;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content for chapter {ChapterId}", chapter.Id);
            return string.Empty;
        }
    }

    private async Task<string> CreateTempFileFromContent(byte[] content, string extension)
    {
        var tempFilePath = Path.GetTempFileName();
        tempFilePath = Path.ChangeExtension(tempFilePath, extension);
        await File.WriteAllBytesAsync(tempFilePath, content);
        return tempFilePath;
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
            chapterAIResult.AIModel = "gemini-pro";

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