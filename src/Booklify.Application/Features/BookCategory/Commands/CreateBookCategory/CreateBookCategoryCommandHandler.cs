using AutoMapper;
using Booklify.Application.Common.DTOs.BookCategory;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Commons;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Booklify.Application.Features.BookCategory.Commands.CreateBookCategory;

public class CreateBookCategoryCommandHandler : IRequestHandler<CreateBookCategoryCommand, Result<CreatedBookCategoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateBookCategoryCommandHandler> _logger;
    
    public CreateBookCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<CreateBookCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }
    
    public async Task<Result<CreatedBookCategoryResponse>> Handle(CreateBookCategoryCommand command, CancellationToken cancellationToken)
    {
        var isUserValid = await _currentUserService.IsUserValidAsync();
        if (!isUserValid)
        {
            return Result<CreatedBookCategoryResponse>.Failure(
                "User is not authenticated", 
                ErrorCode.Unauthorized);
        }

        var currentUserId = _currentUserService.UserId;
        
        try
        {
            // Begin transaction using Unit of Work
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            
            // Create book category
            var bookCategory = _mapper.Map<Domain.Entities.BookCategory>(command.Request);
            BaseEntityExtensions.InitializeBaseEntity(bookCategory, currentUserId);
            
            // Add to repository
            await _unitOfWork.BookCategoryRepository.AddAsync(bookCategory);
            
            // Commit transaction (includes SaveChanges)
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            // Map to response
            var response = _mapper.Map<CreatedBookCategoryResponse>(bookCategory);
            
            _logger.LogInformation("Book category created successfully with ID: {BookCategoryId}", bookCategory.Id);
            return Result<CreatedBookCategoryResponse>.Success(response, "Book category created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book category: {ErrorMessage}", ex.Message);
            
            // Rollback transaction on error
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            return Result<CreatedBookCategoryResponse>.Failure(
                "Error creating book category", 
                ErrorCode.InternalError);
        }
    }
} 