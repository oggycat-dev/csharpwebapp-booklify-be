using Booklify.Application.Common.DTOs.ReadingProgress;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.ReadingProgress.Queries.GetReadingProgress;
 
public record GetReadingProgressQuery(Guid BookId) : IRequest<Result<ReadingProgressResponse?>>; 