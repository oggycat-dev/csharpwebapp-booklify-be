using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Features.Book.BusinessLogic;

namespace Booklify.Application.Features.Book.Queries.GetBooks;

/// <summary>
/// Handler cho GetBooksQuery - admin view với đầy đủ filter options
/// </summary>
public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, Result<PaginatedResult<BookListItemResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly ILogger<GetBooksQueryHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    
    public GetBooksQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        ILogger<GetBooksQueryHandler> logger,
        IBookBusinessLogic bookBusinessLogic)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileService = fileService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
    }
    
    public async Task<Result<PaginatedResult<BookListItemResponse>>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Admin view không fix cứng trạng thái, cho phép filter tất cả
            var result = await _bookBusinessLogic.GetPagedBookListItemsAsync(
                request.Filter, 
                _unitOfWork, 
                _mapper, 
                _fileService);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying book list for admin");
            return Result<PaginatedResult<BookListItemResponse>>.Failure(
                "An error occurred while querying the book list", 
                ErrorCode.InternalError);
        }
    }
} 