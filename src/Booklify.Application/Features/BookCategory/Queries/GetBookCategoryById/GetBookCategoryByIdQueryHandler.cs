using AutoMapper;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.BookCategory.Queries.GetBookCategoryById;

public class GetBookCategoryByIdQueryHandler : IRequestHandler<GetBookCategoryByIdQuery, Result<BookCategoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetBookCategoryByIdQueryHandler> _logger;

    public GetBookCategoryByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetBookCategoryByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BookCategoryResponse>> Handle(GetBookCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the book category with books count
            var category = await _unitOfWork.BookCategoryRepository
                .GetFirstOrDefaultAsync(
                    x => x.Id == request.CategoryId,
                    x => x.Books);

            if (category == null)
            {
                return Result<BookCategoryResponse>.Failure(
                    "Book category not found",
                    ErrorCode.NotFound);
            }

            // Map to response
            var response = _mapper.Map<BookCategoryResponse>(category);
            return Result<BookCategoryResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book category with ID: {CategoryId}", request.CategoryId);
            return Result<BookCategoryResponse>.Failure(
                "Error retrieving book category",
                ErrorCode.InternalError);
        }
    }
} 