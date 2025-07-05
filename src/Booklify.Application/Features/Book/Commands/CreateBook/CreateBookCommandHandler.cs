using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Booklify.Application.Common.DTOs.Book;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Application.Features.Book.BusinessLogic;
using Booklify.Domain.Commons;

namespace Booklify.Application.Features.Book.Commands.CreateBook;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, Result<BookResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateBookCommandHandler> _logger;
    private readonly IFileService _fileService;
    private readonly IEPubService _epubService;
    private readonly IFileBackgroundService _fileBackgroundService;
    private readonly IBookBusinessLogic _bookBusinessLogic;
    private readonly IStorageService _storageService;

    public CreateBookCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreateBookCommandHandler> logger,
        IFileService fileService,
        IEPubService epubService,
        IFileBackgroundService fileBackgroundService,
        IBookBusinessLogic bookBusinessLogic,
        IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _fileService = fileService;
        _epubService = epubService;
        _fileBackgroundService = fileBackgroundService;
        _bookBusinessLogic = bookBusinessLogic;
        _storageService = storageService;
    }

    public async Task<Result<BookResponse>> Handle(CreateBookCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user and get staff information
            var staffResult = await _bookBusinessLogic.ValidateUserAndGetStaffAsync(_currentUserService, _unitOfWork);
            if (!staffResult.IsSuccess)
            {
                return Result<BookResponse>.Failure(staffResult.Message, staffResult.ErrorCode ?? ErrorCode.InternalError);
            }

            var staff = staffResult.Data;
            var currentUserId = _currentUserService.UserId!;

            // Validate book category exists
            var categoryValidation = await _bookBusinessLogic.ValidateBookCategoryAsync(
                command.Request.CategoryId, _unitOfWork);
            
            if (!categoryValidation.IsSuccess)
            {
                return Result<BookResponse>.Failure(categoryValidation.Message, categoryValidation.ErrorCode ?? ErrorCode.ValidationFailed);
            }

            try
            {
                // Begin Unit of Work transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Create and upload file
                var fileResult = await _bookBusinessLogic.CreateBookFileAsync(
                    command.Request.File, "books", currentUserId, _fileService, _storageService, _unitOfWork);
                    
                if (!fileResult.IsSuccess)
                {
                    return Result<BookResponse>.Failure(fileResult.Message, fileResult.ErrorCode ?? ErrorCode.FileUploadFailed);
                }

                var fileInfo = fileResult.Data!;

                // Create book with complete EPUB processing workflow
                var bookResult = await _bookBusinessLogic.CreateBookWithEpubProcessingAsync(
                    command.Request, staff!, fileInfo, currentUserId, _mapper, _unitOfWork,
                    _epubService, _storageService, _logger);
                    
                if (!bookResult.IsSuccess)
                {
                    return Result<BookResponse>.Failure(bookResult.Message, bookResult.ErrorCode ?? ErrorCode.InternalError);
                }

                var book = bookResult.Data!;
                
                // Commit transaction BEFORE background jobs
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                // Queue background job for EPUB chapter processing after successful transaction
                if (_bookBusinessLogic.ShouldProcessEpub(fileInfo.Extension ?? string.Empty))
                {
                    try
                    {
                        _logger.LogInformation("Queuing EPUB chapter processing job for book {BookId}", book.Id);
                        
                        // Read file content for background processing
                        var fileStream = await _storageService.DownloadFileAsync(fileInfo.FilePath!);
                        if (fileStream != null)
                        {
                            using var memoryStream = new MemoryStream();
                            await fileStream.CopyToAsync(memoryStream);
                            var epubFileContent = memoryStream.ToArray();
                            fileStream.Dispose();

                            // Queue the background job with the file content using EPubService
                            _epubService.ProcessEpubFileWithContent(book.Id, currentUserId, epubFileContent, fileInfo.Extension ?? ".epub");
                        }
                        else
                        {
                            _logger.LogWarning("Could not download file for background processing for book {BookId}", book.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to queue EPUB processing job for book {BookId}", book.Id);
                        // Don't fail the entire operation if background job queuing fails
                    }
                }
                
                // Create response
                var response = _mapper.Map<BookResponse>(book);
                response = _bookBusinessLogic.EnrichBookResponse(response, book, _fileService);
                
                return Result<BookResponse>.Success(response, "Tạo sách thành công");
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sách");
            return Result<BookResponse>.Failure("Lỗi khi tạo sách", ErrorCode.InternalError);
        }
    }
} 