using MediatR;
using AutoMapper;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Features.Book.BusinessLogic;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.Book.Queries.GetBooks;

/// <summary>
/// Query để lấy danh sách sách cho user view với thông tin cơ bản và filter cố định trạng thái
/// </summary>
public record GetBookListItemsQuery(BookFilterModel Filter) : IRequest<Result<PaginatedResult<BookListItemResponse>>>;

public class GetBookListItemsQueryHandler : IRequestHandler<GetBookListItemsQuery, Result<PaginatedResult<BookListItemResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    private readonly ILogger<GetBookListItemsQueryHandler> _logger;

    public GetBookListItemsQueryHandler(
        ILogger<GetBookListItemsQueryHandler> logger,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileService fileService,
        IBookBusinessLogic bookBusinessLogic)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _fileService = fileService;
        _bookBusinessLogic = bookBusinessLogic;
    }

    public async Task<Result<PaginatedResult<BookListItemResponse>>> Handle(
        GetBookListItemsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {

        // Đảm bảo luôn fix cứng trạng thái cho user view
        request.Filter.Status = Domain.Enums.EntityStatus.Active;
        request.Filter.ApprovalStatus = Domain.Enums.ApprovalStatus.Approved;
        
        return await _bookBusinessLogic.GetPagedBookListItemsAsync(
            request.Filter,
            _unitOfWork,
            _mapper,
            _fileService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying book list for user");
            return Result<PaginatedResult<BookListItemResponse>>.Failure(
                "An error occurred while querying the book list", 
                ErrorCode.InternalError);
        }
    }
} 