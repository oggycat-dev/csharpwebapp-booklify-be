using System;
using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.BusinessLogic;
using Booklify.Domain.Commons;
using Booklify.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.Book.Commands.ResubmitBook;

public class ResubmitBookCommandHandler : IRequestHandler<ResubmitBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ResubmitBookCommandHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    private readonly IFileService _fileService;

    public ResubmitBookCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<ResubmitBookCommandHandler> logger,
        IBookBusinessLogic bookBusinessLogic,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
        _fileService = fileService;
    }

    public async Task<Result> Handle(ResubmitBookCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate user and get staff info
            var userValidationResult = await _bookBusinessLogic.ValidateUserAndGetStaffAsync(_currentUserService, _unitOfWork);
            if (!userValidationResult.IsSuccess)
            {
                return Result.Failure(userValidationResult.Message, userValidationResult.ErrorCode ?? ErrorCode.Unauthorized);
            }

            var staff = userValidationResult.Data!;
            var currentUserId = _currentUserService.UserId!;

            // 2. Validate that both Staff and Admin can resubmit (Staff for their own books, Admin can help)
            // This allows flexibility for workflow
            if (staff.Position != StaffPosition.Staff && staff.Position != StaffPosition.Administrator)
            {
                return Result.Failure("Chỉ Staff và Admin mới có quyền resubmit sách", ErrorCode.Forbidden);
            }

            // 3. Find existing book
            var existingBook = await _unitOfWork.BookRepository.GetByIdAsync(
                command.BookId);

            if (existingBook == null)
            {
                return Result.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            // 4. Validate that book is in Rejected status
            if (existingBook.ApprovalStatus != ApprovalStatus.Rejected)
            {
                return Result.Failure("Chỉ có thể resubmit sách đang ở trạng thái bị từ chối", ErrorCode.BusinessRuleViolation);
            }

            // 5. For Staff (not Admin), validate they can only resubmit their own books
            // Note: We'll assume book ownership is tracked via CreatedBy field
            if (staff.Position == StaffPosition.Staff && existingBook.CreatedBy != Guid.Parse(currentUserId))
            {
                return Result.Failure("Staff chỉ có thể resubmit sách do mình tạo", ErrorCode.Forbidden);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // 6. Update approval status from Rejected to Pending
                existingBook.ApprovalStatus = ApprovalStatus.Pending;

                // 7. Append resubmit information to existing approval note (preserve history)
                var resubmitNote = !string.IsNullOrEmpty(command.Request.ResubmitNote)
                    ? $"[RESUBMITTED - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {command.Request.ResubmitNote}"
                    : $"[RESUBMITTED - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] Sách đã được gửi lại để phê duyệt";
                
                // Append to existing approval note instead of overwriting to preserve history
                existingBook.ApprovalNote = string.IsNullOrEmpty(existingBook.ApprovalNote) 
                    ? resubmitNote 
                    : $"{existingBook.ApprovalNote}\n{resubmitNote}";

                // 8. Update audit fields
                BaseEntityExtensions.UpdateBaseEntity(existingBook, currentUserId);

                // 9. Save changes
                await _unitOfWork.BookRepository.UpdateAsync(existingBook);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Book {BookId} resubmitted by {UserId} (Staff: {StaffPosition})", 
                    command.BookId, currentUserId, staff.Position);

                // 10. Map to response
                var response = _mapper.Map<BookResponse>(existingBook);
                response = _bookBusinessLogic.EnrichBookResponse(response, existingBook, _fileService);

                return Result.Success("Resubmit sách thành công, đang chờ phê duyệt");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resubmitting book ID: {BookId}", command.BookId);
            return Result.Failure("Lỗi khi resubmit sách", ErrorCode.InternalError);
        }
    }
} 