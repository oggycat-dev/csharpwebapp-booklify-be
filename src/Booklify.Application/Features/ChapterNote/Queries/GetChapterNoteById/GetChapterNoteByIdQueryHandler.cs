using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.ChapterNote.BusinessLogic;

namespace Booklify.Application.Features.ChapterNote.Queries.GetChapterNoteById;

public class GetChapterNoteByIdQueryHandler : IRequestHandler<GetChapterNoteByIdQuery, Result<ChapterNoteResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetChapterNoteByIdQueryHandler> _logger;
    private readonly IChapterNoteBusinessLogic _chapterNoteBusinessLogic;

    public GetChapterNoteByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetChapterNoteByIdQueryHandler> logger,
        IChapterNoteBusinessLogic chapterNoteBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _chapterNoteBusinessLogic = chapterNoteBusinessLogic;
    }

    public async Task<Result<ChapterNoteResponse>> Handle(GetChapterNoteByIdQuery query, CancellationToken cancellationToken)
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

            // Get chapter note by ID using business logic
            return await _chapterNoteBusinessLogic.GetChapterNoteByIdAsync(
                query.NoteId, 
                userProfile.Id, 
                _unitOfWork, 
                _mapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting chapter note details. NoteId: {NoteId}", query.NoteId);
            return Result<ChapterNoteResponse>.Failure("An unexpected error occurred", ErrorCode.InternalError);
        }
    }
}
