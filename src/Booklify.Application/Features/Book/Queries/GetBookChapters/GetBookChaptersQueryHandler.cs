using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Features.Book.BusinessLogic;

namespace Booklify.Application.Features.Book.Queries.GetBookChapters;

public class GetBookChaptersQueryHandler : IRequestHandler<GetBookChaptersQuery, Result<List<ChapterResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetBookChaptersQueryHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    
    public GetBookChaptersQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetBookChaptersQueryHandler> logger,
        IBookBusinessLogic bookBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
    }
    
    public async Task<Result<List<ChapterResponse>>> Handle(GetBookChaptersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _bookBusinessLogic.GetBookChaptersAsync(
                request.BookId,
                _unitOfWork,
                _mapper,
                _currentUserService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chapters for book: {BookId}", request.BookId);
            return Result<List<ChapterResponse>>.Failure("Lỗi khi lấy danh sách chapters", ErrorCode.InternalError);
        }
    }
}
