using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.BusinessLogic;
using Booklify.Domain.Commons;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.Book.Commands.DeleteBook;

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteBookCommandHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    private readonly IFileBackgroundService _fileBackgroundService;
    private readonly IEPubService _epubService;

    public DeleteBookCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DeleteBookCommandHandler> logger,
        IBookBusinessLogic bookBusinessLogic,
        IFileBackgroundService fileBackgroundService,
        IEPubService epubService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
        _fileBackgroundService = fileBackgroundService;
        _epubService = epubService;
    }

    public async Task<Result> Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate user
            var userValidationResult = await _bookBusinessLogic.ValidateUserAndGetStaffAsync(_currentUserService, _unitOfWork);
            if (!userValidationResult.IsSuccess)
            {
                return Result.Failure(userValidationResult.Message, userValidationResult.ErrorCode ?? ErrorCode.Unauthorized);
            }

            var currentUserId = _currentUserService.UserId!;

            // 2. Find existing book
            var existingBook = await _unitOfWork.BookRepository.GetByIdAsync(
                command.BookId,
                b => b.File,
                b => b.Chapters);

            if (existingBook == null)
            {
                return Result.Failure("Không tìm thấy sách", ErrorCode.NotFound);
            }

            if (existingBook.IsDeleted)
            {
                return Result.Failure("Sách đã được xóa trước đó", ErrorCode.ValidationFailed);
            }

            // Prepare job data BEFORE transaction
            var jobData = new BookUpdateJobData
            {
                HasChaptersToDelete = await _unitOfWork.ChapterRepository.AnyAsync(c => c.BookId == existingBook.Id),
                CoverImageToDelete = existingBook.CoverImageUrl,
                FilePathToDelete = existingBook.FilePath,
                FileIdToDelete = existingBook.File?.Id,
                ShouldProcessEpub = false // No EPUB processing needed for deletion
            };

            try
            {
                // Begin Unit of Work transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.BookRepository.SoftDeleteAsync(existingBook, currentUserId);
                
                // Commit transaction BEFORE background jobs
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Queue background cleanup jobs AFTER successful commit
                _bookBusinessLogic.QueueBookBackgroundJobs(
                    jobData,
                    existingBook.Id,
                    currentUserId,
                    _fileBackgroundService,
                    _epubService,
                    _logger);

                return Result.Success("Xóa sách thành công");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book with ID: {BookId}", command.BookId);
            return Result.Failure("Lỗi khi xóa sách", ErrorCode.InternalError);
        }
    }
} 