using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.ChapterNote.BusinessLogic;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.ChapterNote.Commands.CreateChapterNote;

public class CreateChapterNoteCommandHandler : IRequestHandler<CreateChapterNoteCommand, Result<ChapterNoteResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateChapterNoteCommandHandler> _logger;
    private readonly IChapterNoteBusinessLogic _businessLogic;

    public CreateChapterNoteCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreateChapterNoteCommandHandler> logger,
        IChapterNoteBusinessLogic businessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _businessLogic = businessLogic;
    }

    public async Task<Result<ChapterNoteResponse>> Handle(
        CreateChapterNoteCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate user and get profile
            var userResult = await _businessLogic.ValidateUserAndGetProfileAsync(_currentUserService, _unitOfWork);
            if (!userResult.IsSuccess)
            {
                return Result<ChapterNoteResponse>.Failure(userResult.Message, ErrorCode.Unauthorized);
            }

            var userProfile = userResult.Data;

            // Validate chapter access
            var chapterResult = await _businessLogic.ValidateChapterAccessAsync(request.Request.ChapterId, _unitOfWork);
            if (!chapterResult.IsSuccess)
            {
                return Result<ChapterNoteResponse>.Failure(chapterResult.Message, ErrorCode.NotFound);
            }

            ChapterNoteResponse response;
            try
            {
                // Begin transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Create note entity
                var noteResult = await _businessLogic.CreateChapterNoteEntityAsync(
                    request.Request,
                    userProfile,
                    _currentUserService.UserId!,
                    _unitOfWork,
                    _mapper);

                if (!noteResult.IsSuccess)
                {
                    return Result<ChapterNoteResponse>.Failure(noteResult.Message, ErrorCode.ValidationFailed);
                }

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Map to response after successful commit
                response = _mapper.Map<ChapterNoteResponse>(noteResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed while creating chapter note");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw; // Re-throw to be caught by outer try-catch
            }

            return Result<ChapterNoteResponse>.Success(response, "Chapter note created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating chapter note");
            return Result<ChapterNoteResponse>.Failure(
                "An unexpected error occurred while creating the note",
                ErrorCode.InternalError);
        }
    }
}