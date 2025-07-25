using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Features.Book.BusinessLogic;

namespace Booklify.Application.Features.Book.Queries.GetBookDetail;

public class GetBookDetailQueryHandler : IRequestHandler<GetBookDetailQuery, Result<BookDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetBookDetailQueryHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    
    public GetBookDetailQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        ICurrentUserService currentUserService,
        ILogger<GetBookDetailQueryHandler> logger,
        IBookBusinessLogic bookBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileService = fileService;
        _currentUserService = currentUserService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
    }
    
    public async Task<Result<BookDetailResponse>> Handle(GetBookDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _bookBusinessLogic.GetBookDetailByIdAsync(
                request.BookId, 
                _unitOfWork, 
                _mapper, 
                _fileService,
                _currentUserService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book detail by ID: {BookId}", request.BookId);
            return Result<BookDetailResponse>.Failure("Lỗi khi lấy thông tin chi tiết sách", ErrorCode.InternalError);
        }
    }
}
