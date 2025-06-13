using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.BookCategory.Queries.GetBookCategories;

public class GetBookCategoriesQueryHandler : IRequestHandler<GetBookCategoriesQuery, PaginatedResult<BookCategoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetBookCategoriesQueryHandler> _logger;
    
    public GetBookCategoriesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetBookCategoriesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<PaginatedResult<BookCategoryResponse>> Handle(GetBookCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var filter = request.Filter ?? new BookCategoryFilterModel();
            
            // Get paged book categories from repository
            var (bookCategories, totalCount) = await _unitOfWork.BookCategoryRepository.GetPagedBookCategoriesAsync(filter);
            
            // Map to response DTOs using AutoMapper
            var bookCategoryResponses = _mapper.Map<List<BookCategoryResponse>>(bookCategories);
            
            // Return paginated result
            return PaginatedResult<BookCategoryResponse>.Success(
                bookCategoryResponses, 
                filter.PageNumber,
                filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying book category list");
            return PaginatedResult<BookCategoryResponse>.Failure("An error occurred while querying the book category list", ErrorCode.InternalError);
        }
    }
} 