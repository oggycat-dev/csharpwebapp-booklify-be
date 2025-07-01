using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.Book.Commands.IncrementBookViews;

public class IncrementBookViewsCommandHandler : IRequestHandler<IncrementBookViewsCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IncrementBookViewsCommandHandler> _logger;

    public IncrementBookViewsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<IncrementBookViewsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(IncrementBookViewsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get book
            var book = await _unitOfWork.BookRepository.GetByIdAsync(request.BookId);
            if (book == null)
            {
                return Result.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Increment views
                book.TotalViews++;

                // Save changes
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Incremented views for book {BookId}. New total: {TotalViews}", request.BookId, book.TotalViews);
                return Result.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing views for book {BookId}", request.BookId);
            return Result.Failure("Lỗi khi cập nhật lượt xem", ErrorCode.InternalError);
        }
    }
} 