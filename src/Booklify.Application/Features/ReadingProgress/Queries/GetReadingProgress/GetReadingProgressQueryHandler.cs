using AutoMapper;
using Booklify.Application.Common.DTOs.ReadingProgress;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.ReadingProgress.Queries.GetReadingProgress;

public class GetReadingProgressQueryHandler : IRequestHandler<GetReadingProgressQuery, Result<ReadingProgressResponse?>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetReadingProgressQueryHandler> _logger;

    public GetReadingProgressQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetReadingProgressQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ReadingProgressResponse?>> Handle(GetReadingProgressQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate user
            var isUserValid = await _currentUserService.IsUserValidAsync();
            if (!isUserValid)
            {
                return Result<ReadingProgressResponse?>.Failure("User is not authorized", ErrorCode.Unauthorized);
            }

            var currentUserId = _currentUserService.UserId;

            var userProfile = await _unitOfWork.UserProfileRepository.GetFirstOrDefaultAsync(x => x.IdentityUserId == currentUserId);
            if (userProfile == null)
            {
                return Result<ReadingProgressResponse?>.Failure("User profile not found", ErrorCode.NotFound);
            }

            // 2. Validate book exists
            var bookExists = await _unitOfWork.BookRepository.AnyAsync(x => x.Id == request.BookId);
            if (!bookExists)
            {
                return Result<ReadingProgressResponse?>.Failure("Book not found", ErrorCode.NotFound);
            }

            // 3. Get reading progress
            var readingProgress = await _unitOfWork.ReadingProgressRepository.GetFirstOrDefaultAsync(
                x => x.UserId == userProfile.Id && x.BookId == request.BookId,
                x => x.Book, x => x.CurrentChapter, x => x.ChapterProgresses);

            if (readingProgress == null)
            {
                return Result<ReadingProgressResponse?>.Success(null, "No reading progress found for this book");
            }

            // 4. Map to response
            var response = _mapper.Map<ReadingProgressResponse>(readingProgress);

            // 5. Manual calculation for lists (if needed)
            if (readingProgress.ChapterProgresses?.Any() == true)
            {
                response.CompletedChapterIds = readingProgress.ChapterProgresses
                    .Where(cp => cp.IsCompleted)
                    .Select(cp => cp.ChapterId)
                    .ToList();

                response.AccessedChapterIds = readingProgress.ChapterProgresses
                    .Select(cp => cp.ChapterId)
                    .ToList();

                // Map chapter progresses
                response.ChapterProgresses = readingProgress.ChapterProgresses
                    .Select(cp => _mapper.Map<ChapterReadingProgressResponse>(cp))
                    .OrderBy(cp => cp.ChapterOrder)
                    .ToList();
            }

            return Result<ReadingProgressResponse?>.Success(response, "Reading progress retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reading progress for book {BookId}", request.BookId);
            return Result<ReadingProgressResponse?>.Failure("Error retrieving reading progress", ErrorCode.InternalError);
        }
    }
} 