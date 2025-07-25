using AutoMapper;
using MediatR;
using Booklify.Application.Common.DTOs.ChapterNote;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.ChapterNote.BusinessLogic;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.ChapterNote.Queries.GetChapterNotes;

public class GetChapterNotesQueryHandler : IRequestHandler<GetChapterNotesQuery, Result<PaginatedResult<ChapterNoteListItemResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IChapterNoteBusinessLogic _businessLogic;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetChapterNotesQueryHandler> _logger;

    public GetChapterNotesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IChapterNoteBusinessLogic businessLogic,
        ICurrentUserService currentUserService,
        ILogger<GetChapterNotesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _businessLogic = businessLogic;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<ChapterNoteListItemResponse>>> Handle(
        GetChapterNotesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _businessLogic.GetPagedChapterNotesAsync(
                request.Filter,
                _currentUserService,
                _unitOfWork,
                _mapper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error occurred while getting chapter notes. Filter: {@Filter}", 
                request.Filter);
            return Result<PaginatedResult<ChapterNoteListItemResponse>>.Failure(
                "An unexpected error occurred", 
                ErrorCode.InternalError);
        }
    }
}
