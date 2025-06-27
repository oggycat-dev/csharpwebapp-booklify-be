using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Features.Book.BusinessLogic;

namespace Booklify.Application.Features.Book.Queries.GetBooks;

public class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, PaginatedResult<BookResponse>>
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
    
    public async Task<PaginatedResult<BookResponse>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bookBusinessLogic.GetPagedBooksAsync(
                request.Filter, 
                _unitOfWork, 
                _mapper, 
                _fileService);
            
            if (!result.IsSuccess)
            {
                return PaginatedResult<BookResponse>.Failure(result.Message, result.ErrorCode ?? ErrorCode.InternalError);
            }
            
            return result.Data!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying book list");
            return PaginatedResult<BookResponse>.Failure("An error occurred while querying the book list", ErrorCode.InternalError);
        }
    }
} 