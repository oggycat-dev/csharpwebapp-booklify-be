using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.ChapterNote.BusinessLogic;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.ChapterNote.Commands.UpdateChapterNote;

public class UpdateChapterNoteCommandHandler : IRequestHandler<UpdateChapterNoteCommand, Result<ChapterNoteResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateChapterNoteCommandHandler> _logger;
    private readonly IChapterNoteBusinessLogic _chapterNoteBusinessLogic;

    public UpdateChapterNoteCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateChapterNoteCommandHandler> logger,
        IChapterNoteBusinessLogic chapterNoteBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _chapterNoteBusinessLogic = chapterNoteBusinessLogic;
    }

    public async Task<Result<ChapterNoteResponse>> Handle(UpdateChapterNoteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user and get user profile
            var userProfileResult = await _chapterNoteBusinessLogic.ValidateUserAndGetProfileAsync(
                _currentUserService, _unitOfWork);

            if (!userProfileResult.IsSuccess)
            {
                return Result<ChapterNoteResponse>.Failure(userProfileResult.Message, userProfileResult.ErrorCode ?? ErrorCode.Unauthorized);
            }

            var userProfile = userProfileResult.Data!;
            ChapterNoteResponse response;

            try
            {
                // Begin transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Update chapter note using business logic
                var result = await _chapterNoteBusinessLogic.UpdateChapterNoteAsync(
                    command.NoteId,
                    command.Request,
                    userProfile.Id,
                    _currentUserService.UserId!,
                    _unitOfWork);

                if (!result.IsSuccess)
                {
                    return Result<ChapterNoteResponse>.Failure(result.Message, result.ErrorCode ?? ErrorCode.ValidationFailed);
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Map to response after successful commit
                response = _mapper.Map<ChapterNoteResponse>(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed while updating chapter note");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw; // Re-throw to be caught by outer try-catch
            }

            return Result<ChapterNoteResponse>.Success(response, "Chapter note updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating chapter note");
            return Result<ChapterNoteResponse>.Failure(
                "An unexpected error occurred while updating the note",
                ErrorCode.InternalError);
        }
    }
}
