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

namespace Booklify.Application.Features.Book.Commands.ManageBookStatus;

public class ManageBookStatusCommandHandler : IRequestHandler<ManageBookStatusCommand, Result<BookResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ManageBookStatusCommandHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    private readonly IFileService _fileService;

    public ManageBookStatusCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<ManageBookStatusCommandHandler> logger,
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

    public async Task<Result<BookResponse>> Handle(ManageBookStatusCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate user and get staff info
            var userValidationResult = await _bookBusinessLogic.ValidateUserAndGetStaffAsync(_currentUserService, _unitOfWork);
            if (!userValidationResult.IsSuccess)
            {
                return Result<BookResponse>.Failure(userValidationResult.Message, userValidationResult.ErrorCode ?? ErrorCode.Unauthorized);
            }

            var staff = userValidationResult.Data!;
            var currentUserId = _currentUserService.UserId!;

            // 2. Validate that only Admin can manage book status
            if (staff.Position != StaffPosition.Administrator)
            {
                return Result<BookResponse>.Failure("Chỉ Admin mới có quyền quản lý trạng thái sách", ErrorCode.Forbidden);
            }

            // 3. Find existing book
            var existingBook = await _unitOfWork.BookRepository.GetByIdAsync(
                command.BookId,
                b => b.Category,
                b => b.File,
                b => b.Chapters);

            if (existingBook == null)
            {
                return Result<BookResponse>.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            var request = command.Request;
            bool hasChanges = false;

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Update book status (Active/Inactive)
                if (request.Status.HasValue && request.Status != existingBook.Status)
                {
                    existingBook.Status = request.Status.Value;
                    hasChanges = true;
                }

                // Update approval status with business logic validation
                if (request.ApprovalStatus.HasValue && request.ApprovalStatus != existingBook.ApprovalStatus)
                {
                    // Validate approval status transition
                    var validationResult = ValidateApprovalStatusTransition(existingBook.ApprovalStatus, request.ApprovalStatus.Value);
                    if (!validationResult.IsValid)
                    {
                        return Result<BookResponse>.Failure(validationResult.ErrorMessage, ErrorCode.InvalidOperation);
                    }

                    existingBook.ApprovalStatus = request.ApprovalStatus.Value;
                    hasChanges = true;
                }

                // Append approval status changes to existing note (preserve full history)
                if (request.ApprovalStatus.HasValue)
                {
                    if (request.ApprovalStatus.Value == ApprovalStatus.Approved)
                    {
                        // Khi approve, luôn append vào lịch sử
                        var approvedNote = string.IsNullOrEmpty(request.ApprovalNote)
                            ? $"[APPROVED - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]"
                            : $"[APPROVED - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {request.ApprovalNote}";
                        
                        // Append to preserve history
                        existingBook.ApprovalNote = string.IsNullOrEmpty(existingBook.ApprovalNote) 
                            ? approvedNote 
                            : $"{existingBook.ApprovalNote}\n{approvedNote}";
                        hasChanges = true;
                    }
                    else if (request.ApprovalStatus.Value == ApprovalStatus.Rejected)
                    {
                        // Khi reject, luôn append timestamp và lý do từ chối vào lịch sử
                        var rejectionReason = !string.IsNullOrEmpty(request.ApprovalNote) 
                            ? request.ApprovalNote 
                            : "Sách bị từ chối";
                        var rejectedNote = $"[REJECTED - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {rejectionReason}";
                        
                        // Append to preserve history
                        existingBook.ApprovalNote = string.IsNullOrEmpty(existingBook.ApprovalNote) 
                            ? rejectedNote 
                            : $"{existingBook.ApprovalNote}\n{rejectedNote}";
                        hasChanges = true;
                    }
                    else if (request.ApprovalStatus.Value == ApprovalStatus.Pending)
                    {
                        // Khi chuyển về Pending (nếu có), cũng nên ghi lại lịch sử
                        if (!string.IsNullOrEmpty(request.ApprovalNote))
                        {
                            var pendingNote = $"[PENDING - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {request.ApprovalNote}";
                            existingBook.ApprovalNote = string.IsNullOrEmpty(existingBook.ApprovalNote) 
                                ? pendingNote 
                                : $"{existingBook.ApprovalNote}\n{pendingNote}";
                            hasChanges = true;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(request.ApprovalNote))
                {
                    // Trường hợp chỉ cập nhật approval note mà không thay đổi approval status
                    // Append general note với timestamp
                    var generalNote = $"[NOTE - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {request.ApprovalNote}";
                    existingBook.ApprovalNote = string.IsNullOrEmpty(existingBook.ApprovalNote) 
                        ? generalNote 
                        : $"{existingBook.ApprovalNote}\n{generalNote}";
                    hasChanges = true;
                }

                // Update premium status
                if (request.IsPremium.HasValue && request.IsPremium != existingBook.IsPremium)
                {
                    existingBook.IsPremium = request.IsPremium.Value;
                    hasChanges = true;
                }

                if (!hasChanges)
                {
                    // Map current book to response when no changes
                    var currentResponse = _mapper.Map<BookResponse>(existingBook);
                    currentResponse = _bookBusinessLogic.EnrichBookResponse(currentResponse, existingBook, _fileService);
                    return Result<BookResponse>.Success(currentResponse, "Không có thay đổi nào được phát hiện");
                }

                // Update audit fields
                BaseEntityExtensions.UpdateBaseEntity(existingBook, currentUserId);

                // Save changes
                await _unitOfWork.BookRepository.UpdateAsync(existingBook);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Map to response
                var response = _mapper.Map<BookResponse>(existingBook);
                response = _bookBusinessLogic.EnrichBookResponse(response, existingBook, _fileService);

                return Result<BookResponse>.Success(response, "Cập nhật trạng thái sách thành công");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing book status for book ID: {BookId}", command.BookId);
            return Result<BookResponse>.Failure("Lỗi khi cập nhật trạng thái sách", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Validates approval status transition according to business rules
    /// </summary>
    private static (bool IsValid, string ErrorMessage) ValidateApprovalStatusTransition(ApprovalStatus currentStatus, ApprovalStatus newStatus)
    {
        // If status is not changing, it's valid
        if (currentStatus == newStatus)
        {
            return (true, string.Empty);
        }

        // Business rules for approval status transitions:
        // 1. From Pending (0) -> can go to Approved (1) or Rejected (2)
        // 2. From Approved (1) -> can only go to Rejected (2)
        // 3. From Rejected (2) -> can only go to Approved (1)
        // 4. Cannot go back to Pending (0) once approved or rejected

        switch (currentStatus)
        {
            case ApprovalStatus.Pending:
                // From Pending, can go to Approved or Rejected
                if (newStatus == ApprovalStatus.Approved || newStatus == ApprovalStatus.Rejected)
                {
                    return (true, string.Empty);
                }
                break;

            case ApprovalStatus.Approved:
                // From Approved, can only go to Rejected
                if (newStatus == ApprovalStatus.Rejected)
                {
                    return (true, string.Empty);
                }
                // Cannot go back to Pending
                if (newStatus == ApprovalStatus.Pending)
                {
                    return (false, "Không thể chuyển từ 'Đã duyệt' về 'Chờ duyệt'. Sách đã được xử lý không thể quay về trạng thái chờ duyệt.");
                }
                break;

            case ApprovalStatus.Rejected:
                // From Rejected, can only go to Approved
                if (newStatus == ApprovalStatus.Approved)
                {
                    return (true, string.Empty);
                }
                // Cannot go back to Pending
                if (newStatus == ApprovalStatus.Pending)
                {
                    return (false, "Không thể chuyển từ 'Từ chối' về 'Chờ duyệt'. Sách bị từ chối không thể quay về trạng thái chờ duyệt.");
                }
                break;
        }

        return (false, $"Không thể chuyển từ '{GetApprovalStatusText(currentStatus)}' sang '{GetApprovalStatusText(newStatus)}'.");
    }

    /// <summary>
    /// Get human-readable text for approval status
    /// </summary>
    private static string GetApprovalStatusText(ApprovalStatus status)
    {
        return status switch
        {
            ApprovalStatus.Pending => "Chờ duyệt",
            ApprovalStatus.Approved => "Đã duyệt",
            ApprovalStatus.Rejected => "Từ chối",
            _ => "Không xác định"
        };
    }
} 