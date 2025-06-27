using AutoMapper;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.BusinessLogic;
using Booklify.Domain.Commons;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Features.Book.Commands.UpdateBook;

public class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, Result<BookResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateBookCommandHandler> _logger;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    private readonly IFileService _fileService;
    private readonly IStorageService _storageService;
    private readonly IFileBackgroundService _fileBackgroundService;
    private readonly IEPubService _epubService;

    public UpdateBookCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<UpdateBookCommandHandler> logger,
        IBookBusinessLogic bookBusinessLogic,
        IFileService fileService,
        IStorageService storageService,
        IFileBackgroundService fileBackgroundService,
        IEPubService epubService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _bookBusinessLogic = bookBusinessLogic;
        _fileService = fileService;
        _storageService = storageService;
        _fileBackgroundService = fileBackgroundService;
        _epubService = epubService;
    }

    public async Task<Result<BookResponse>> Handle(UpdateBookCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate user and get staff info
        var userValidationResult = await _bookBusinessLogic.ValidateUserAndGetStaffAsync(_currentUserService, _unitOfWork);
        if (!userValidationResult.IsSuccess)
        {
            return Result<BookResponse>.Failure(userValidationResult.Message, userValidationResult.ErrorCode ?? ErrorCode.Unauthorized);
        }

        var staff = userValidationResult.Data!;
        var currentUserId = _currentUserService.UserId!;

        // 2. Find existing book
        var existingBook = await _unitOfWork.BookRepository.GetByIdAsync(
            command.BookId,
            b => b.Category,
            b => b.File,
            b => b.Chapters);

        if (existingBook == null)
        {
            return Result<BookResponse>.Failure("Không tìm thấy sách", ErrorCode.NotFound);
        }

        try
        {
            // Prepare background job data BEFORE transaction
            var request = command.Request;
            bool hasChanges = false;
            bool hasNewFile = request.File != null;

            // Only prepare job data if there's a new file to process
            BookUpdateJobData? jobData = null;
            if (hasNewFile)
            {
            var jobDataResult = await _bookBusinessLogic.PrepareBookUpdateJobDataAsync(existingBook, hasNewFile, _unitOfWork);
            if (!jobDataResult.IsSuccess)
            {
                return Result<BookResponse>.Failure(jobDataResult.Message, jobDataResult.ErrorCode ?? ErrorCode.InternalError);
            }
                jobData = jobDataResult.Data!;
            }

            // Begin Unit of Work transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Update text fields
            if (!string.IsNullOrWhiteSpace(request.Title) && request.Title != existingBook.Title)
            {
                existingBook.Title = request.Title;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Author) && request.Author != existingBook.Author)
            {
                existingBook.Author = request.Author;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.ISBN) && request.ISBN != existingBook.ISBN)
            {
                // Check for duplicate ISBN
                var isDuplicate = await _unitOfWork.BookRepository.AnyAsync(
                    b => b.ISBN == request.ISBN && b.Id != command.BookId);
                if (isDuplicate)
                {
                    return Result<BookResponse>.Failure("ISBN đã tồn tại", ErrorCode.ValidationFailed);
                }
                existingBook.ISBN = request.ISBN;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Publisher) && request.Publisher != existingBook.Publisher)
            {
                existingBook.Publisher = request.Publisher;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description != existingBook.Description)
            {
                existingBook.Description = request.Description;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Tags) && request.Tags != existingBook.Tags)
            {
                existingBook.Tags = request.Tags;
                hasChanges = true;
            }

            // Update enum fields
            if (request.CategoryId.HasValue && request.CategoryId != existingBook.CategoryId)
            {
                // Validate category exists
                var categoryValidationResult = await _bookBusinessLogic.ValidateBookCategoryAsync(request.CategoryId.Value, _unitOfWork);
                if (!categoryValidationResult.IsSuccess)
                {
                    return Result<BookResponse>.Failure(categoryValidationResult.Message, categoryValidationResult.ErrorCode ?? ErrorCode.NotFound);
                }
                existingBook.CategoryId = request.CategoryId.Value;
                hasChanges = true;
            }

            if (request.Status.HasValue && request.Status != existingBook.Status)
            {
                existingBook.Status = request.Status.Value;
                hasChanges = true;
            }

            if (request.IsPremium.HasValue && request.IsPremium != existingBook.IsPremium)
            {
                existingBook.IsPremium = request.IsPremium.Value;
                hasChanges = true;
            }

            if (request.PublishedDate != existingBook.PublishedDate)
            {
                existingBook.PublishedDate = request.PublishedDate;
                hasChanges = true;
            }

            // Handle file update
            Domain.Entities.FileInfo? newFileInfo = null;
            if (hasNewFile)
            {
                var fileResult = await _bookBusinessLogic.CreateBookFileAsync(
                    request.File!, "books", currentUserId, _fileService, _storageService, _unitOfWork);
                if (!fileResult.IsSuccess)
                {
                    return Result<BookResponse>.Failure(fileResult.Message, fileResult.ErrorCode ?? ErrorCode.FileUploadFailed);
                }

                newFileInfo = fileResult.Data!;
                existingBook.FilePath = newFileInfo.FilePath;
                existingBook.File = newFileInfo;
                hasChanges = true;

                // Check if should process EPUB - only set when there's a new file
                var fileExtension = Path.GetExtension(newFileInfo.FilePath);
                jobData!.ShouldProcessEpub = _bookBusinessLogic.ShouldProcessEpub(fileExtension);
            }

            if (!hasChanges)
            {
                return Result<BookResponse>.Failure("Không có thay đổi nào được phát hiện", ErrorCode.ValidationFailed);
            }

            // Update audit fields
            BaseEntityExtensions.UpdateBaseEntity(existingBook, currentUserId);

            // Save changes
            await _unitOfWork.BookRepository.UpdateAsync(existingBook);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Commit transaction BEFORE background jobs
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Queue background jobs AFTER successful commit - ONLY if there's a new file
            if (hasNewFile && jobData != null)
            {
            _bookBusinessLogic.QueueBookBackgroundJobs(
                jobData, 
                existingBook.Id, 
                currentUserId, 
                _fileBackgroundService, 
                _epubService, 
                _logger);
            }

            // Map to response
            var response = _mapper.Map<BookResponse>(existingBook);
            response = _bookBusinessLogic.EnrichBookResponse(response, existingBook, _fileService);

            return Result<BookResponse>.Success(response, "Cập nhật sách thành công");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error updating book with ID: {BookId}", command.BookId);
            return Result<BookResponse>.Failure("Lỗi khi cập nhật sách", ErrorCode.InternalError);
        }
    }
} 