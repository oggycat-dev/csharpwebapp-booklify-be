using AutoMapper;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Booklify.Application.Features.BookCategory.Commands.UpdateBookCategory;

public class UpdateBookCategoryCommandHandler : IRequestHandler<UpdateBookCategoryCommand, Result<BookCategoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateBookCategoryCommandHandler> _logger;

    public UpdateBookCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateBookCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<BookCategoryResponse>> Handle(UpdateBookCategoryCommand command, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<BookCategoryResponse>.Failure(
                "User is not authenticated",
                ErrorCode.Unauthorized);
        }

        // 1. Find existing book category with books for business rule validation
        var existingCategory = await _unitOfWork.BookCategoryRepository
            .GetFirstOrDefaultAsync(
                x => x.Id == command.CategoryId,
                x => x.Books);

        if (existingCategory == null)
        {
            return Result<BookCategoryResponse>.Failure(
                "Book category not found",
                ErrorCode.NotFound);
        }

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            // 2. Apply partial updates (only update fields that are provided)
            var request = command.Request;
            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != existingCategory.Name)
            {
                // Check if name already exists
                var nameExists = await _unitOfWork.BookCategoryRepository
                    .AnyAsync(x => x.Name == request.Name && x.Id != command.CategoryId);
                if (nameExists)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<BookCategoryResponse>.Failure(
                        "Category name already exists",
                        ErrorCode.ValidationFailed);
                }

                existingCategory.Name = request.Name;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description != existingCategory.Description)
            {
                existingCategory.Description = request.Description;
                hasChanges = true;
            }

            if (request.IsActive.HasValue)
            {
                var newStatus = request.IsActive.Value ? EntityStatus.Active : EntityStatus.Inactive;
                if (existingCategory.Status != newStatus)
                {
                    // Business rule: Cannot set category to inactive if it contains books
                    if (newStatus == EntityStatus.Inactive && existingCategory.Books != null && existingCategory.Books.Any())
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<BookCategoryResponse>.Failure(
                            "Cannot set book category to inactive while it contains books. Please move or delete all books in this category first.",
                            ErrorCode.BusinessRuleViolation);
                    }

                    existingCategory.Status = newStatus;
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<BookCategoryResponse>.Success(    
                    _mapper.Map<BookCategoryResponse>(existingCategory));
            }

            // 3. Update modified date
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

            // 5. Map to response
            var response = _mapper.Map<BookCategoryResponse>(existingCategory);
            return Result<BookCategoryResponse>.Success(response, "Book category updated successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error updating book category with ID: {CategoryId}", command.CategoryId);
            return Result<BookCategoryResponse>.Failure(
                "Error updating book category",
                ErrorCode.InternalError);
        }
    }
} 