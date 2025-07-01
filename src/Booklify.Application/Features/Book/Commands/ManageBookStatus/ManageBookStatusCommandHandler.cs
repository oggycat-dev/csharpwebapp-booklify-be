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

                // Update approval status
                if (request.ApprovalStatus.HasValue && request.ApprovalStatus != existingBook.ApprovalStatus)
                {
                    existingBook.ApprovalStatus = request.ApprovalStatus.Value;
                    hasChanges = true;
                }

                // Update approval note
                if (!string.IsNullOrEmpty(request.ApprovalNote) && request.ApprovalNote != existingBook.ApprovalNote)
                {
                    existingBook.ApprovalNote = request.ApprovalNote;
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
                    return Result<BookResponse>.Success(null, "Không có thay đổi nào được phát hiện");
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
} 