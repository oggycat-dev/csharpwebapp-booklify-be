using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Features.Book.BusinessLogic;

namespace Booklify.Application.Features.Book.Queries.GetBookById;

public class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, Result<BookResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetBookByIdQueryHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    
    public GetBookByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        ICurrentUserService currentUserService,
        ILogger<GetBookByIdQueryHandler> logger,
        IBookBusinessLogic bookBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileService = fileService;
        _currentUserService = currentUserService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
    }
    
    public async Task<Result<BookResponse>> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _bookBusinessLogic.GetBookByIdAsync(
                request.BookId, 
                _unitOfWork, 
                _mapper, 
                _fileService,
                _currentUserService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book by ID: {BookId}", request.BookId);
            return Result<BookResponse>.Failure("Lỗi khi lấy thông tin sách", ErrorCode.InternalError);
        }
    }
} 