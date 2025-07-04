using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.References.Queries;

public class BookCategoryDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Query to get list of book categories for dropdown
/// </summary>
public record GetBookCategoriesQuery : IRequest<Result<List<BookCategoryDto>>>;

/// <summary>
/// Handler for GetBookCategoriesQuery
/// </summary>
public class GetBookCategoriesQueryHandler : IRequestHandler<GetBookCategoriesQuery, Result<List<BookCategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetBookCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result<List<BookCategoryDto>>> Handle(GetBookCategoriesQuery request, CancellationToken cancellationToken)
    {
        // Get active book categories from repository with ordering
        var categories = await _unitOfWork.BookCategoryRepository.FindAsync(
            c => c.Status == EntityStatus.Active,
            c => c.Name);
            
        // Map to DTO
        var categoryDtos = categories.Select(c => new BookCategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }).OrderBy(c => c.Name).ToList();
            
        return Result<List<BookCategoryDto>>.Success(categoryDtos);
    }
}
