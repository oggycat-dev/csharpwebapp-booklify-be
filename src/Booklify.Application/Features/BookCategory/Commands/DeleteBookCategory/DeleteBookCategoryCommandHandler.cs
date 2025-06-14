using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Booklify.Application.Features.BookCategory.Commands.DeleteBookCategory;

public class DeleteBookCategoryCommandHandler : IRequestHandler<DeleteBookCategoryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteBookCategoryCommandHandler> _logger;

    public DeleteBookCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteBookCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteBookCategoryCommand command, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        // 1. Find existing book category with books
        var existingCategory = await _unitOfWork.BookCategoryRepository
            .GetFirstOrDefaultAsync(
                x => x.Id == command.CategoryId,
                x => x.Books);

        if (existingCategory == null)
        {
            return Result.Failure(
                "Book category not found",
                ErrorCode.NotFound);
        }

        // 2. Business rule: Cannot delete category if it contains books
        if (existingCategory.Books != null && existingCategory.Books.Any())
        {
            return Result.Failure(
                "Cannot delete book category that contains books. Please move or delete all books in this category first.",
                ErrorCode.BusinessRuleViolation);
        }

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            // 3. Soft delete - set status to inactive and update modified fields
            existingCategory.Status = EntityStatus.Inactive;
            existingCategory.ModifiedAt = DateTime.UtcNow;
            
            var currentUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
            {
                existingCategory.ModifiedBy = userGuid;
            }

            // 4. Save changes
            await _unitOfWork.BookCategoryRepository.UpdateAsync(existingCategory);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Success("Book category deleted successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting book category with ID: {CategoryId}", command.CategoryId);
            return Result.Failure(
                "Error deleting book category",
                ErrorCode.InternalError);
        }
    }
} 