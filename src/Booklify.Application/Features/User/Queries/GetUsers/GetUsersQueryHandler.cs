using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Booklify.Application.Common.DTOs.User;
using Booklify.Application.Common.Models;
using Booklify.Application.Common.Interfaces;

namespace Booklify.Application.Features.User.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResult<UserResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUsersQueryHandler> _logger;
    
    public GetUsersQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUsersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<PaginatedResult<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var filter = request.Filter ?? new UserFilterModel();
            
            // Get paged users from repository
            var (users, totalCount) = await _unitOfWork.UserProfileRepository.GetPagedUsersAsync(filter);
            
            // Map to response DTOs using AutoMapper
            var userResponses = _mapper.Map<List<UserResponse>>(users);
            
            // Set additional information from IdentityUser and UserSubscriptions for each user
            foreach (var userResponse in userResponses)
            {
                var user = users.FirstOrDefault(u => u.Id == userResponse.Id);
                if (user?.IdentityUser != null)
                {
                    userResponse.Username = user.IdentityUser.UserName ?? string.Empty;
                    userResponse.Email = user.IdentityUser.Email ?? string.Empty;
                    userResponse.IsActive = user.IdentityUser.IsActive;
                }
                
                // Set subscription status
                var hasActiveSubscription = user?.UserSubscriptions?
                    .Any(us => us.IsActive && us.EndDate > DateTime.UtcNow) ?? false;
                userResponse.HasActiveSubscription = hasActiveSubscription;
            }
            
            // Return paginated result
            return PaginatedResult<UserResponse>.Success(
                userResponses, 
                filter.PageNumber,
                filter.PageSize,
                totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying user list");
            return PaginatedResult<UserResponse>.Failure("An error occurred while querying the user list", ErrorCode.InternalError);
        }
    }
} 