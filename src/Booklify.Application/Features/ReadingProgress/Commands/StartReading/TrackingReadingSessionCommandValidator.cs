using Booklify.Application.Common.DTOs.ReadingProgress;
using Booklify.Application.Common.Interfaces;
using FluentValidation;

namespace Booklify.Application.Features.ReadingProgress.Commands.StartReading;

public class TrackingReadingSessionCommandValidator : AbstractValidator<TrackingReadingSessionCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    
    public TrackingReadingSessionCommandValidator(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;

        RuleFor(x => x.Request.BookId)
            .NotEmpty().WithMessage("Book ID is required");
        RuleFor(x => x.Request.ChapterId)
            .NotEmpty().WithMessage("Chapter ID is required")
            .MustAsync(async (command, chapterId, cancellationToken) =>
            {
                var chapter = await _unitOfWork.ChapterRepository.GetFirstOrDefaultAsync(
                    x => x.Id == chapterId);
                
                if (chapter == null) return false;
                
                // Ensure chapter belongs to the specified book
                return chapter.BookId == command.Request.BookId;
            }).WithMessage("Chapter not found or does not belong to the specified book");

        RuleFor(x => x.Request.CurrentCfi)
            .MaximumLength(1000).WithMessage("Current CFI cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.CurrentCfi));
            
        // Note: Chapter completion validation is handled in the handler with early return
        // Business Rule: Chapter completion is immutable (once true, always true)
        // Performance optimization: Early return for completed chapters to avoid unnecessary processing
    }
}   