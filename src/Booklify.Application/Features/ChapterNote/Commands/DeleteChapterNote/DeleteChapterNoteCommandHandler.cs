using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.ChapterNote.BusinessLogic;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.ChapterNote.Commands.DeleteChapterNote;

public class DeleteChapterNoteCommandHandler : IRequestHandler<DeleteChapterNoteCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteChapterNoteCommandHandler> _logger;
    private readonly IChapterNoteBusinessLogic _chapterNoteBusinessLogic;

    public DeleteChapterNoteCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteChapterNoteCommandHandler> logger,
        IChapterNoteBusinessLogic chapterNoteBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _chapterNoteBusinessLogic = chapterNoteBusinessLogic;
    }

    public async Task<Result<bool>> Handle(DeleteChapterNoteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user and get user profile
            var userProfileResult = await _chapterNoteBusinessLogic.ValidateUserAndGetProfileAsync(
                _currentUserService, _unitOfWork);
            
            if (!userProfileResult.IsSuccess)
            {
                return Result<bool>.Failure(userProfileResult.Message, userProfileResult.ErrorCode ?? ErrorCode.Unauthorized);
            }

            var userProfile = userProfileResult.Data!;
            var currentUserId = _currentUserService.UserId!;

            try
            {
                // Begin transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Delete chapter note using business logic
                var result = await _chapterNoteBusinessLogic.DeleteChapterNoteAsync(
                    command.NoteId, 
                    userProfile.Id, 
                    currentUserId, 
                    _unitOfWork);
                    
                if (!result.IsSuccess)
                {
                    return Result<bool>.Failure(result.Message, result.ErrorCode ?? ErrorCode.ValidationFailed);
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed while deleting chapter note");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw; // Re-throw to be caught by outer try-catch
            }

            return Result<bool>.Success(true, "Chapter note deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while deleting chapter note");
            return Result<bool>.Failure(
                "An unexpected error occurred while deleting the note",
                ErrorCode.InternalError);
        }
    }
}
