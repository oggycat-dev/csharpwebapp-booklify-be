using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Booklify.Application.Common.DTOs.User;
using Booklify.Infrastructure.Repositories.Extensions;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Booklify.Infrastructure.Repositories;

public class UserProfileRepository : GenericRepository<UserProfile>, IUserProfileRepository
{
    private readonly BooklifyDbContext _context;
    
    public UserProfileRepository(BooklifyDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<(List<UserProfile> Users, int TotalCount)> GetPagedUsersAsync(UserFilterModel filter)
    {
        // Build filter predicate
        Expression<Func<UserProfile, bool>> predicate = BuildFilterPredicate(filter);
        
        // Determine sorting property
        var orderByExpression = GetOrderByExpression(filter.SortBy);

        // Get paged data from base repository with includes for IdentityUser and UserSubscriptions
        return await GetPagedAsync(
            predicate,
            orderByExpression,
            filter.IsAscending,
            filter.PageNumber,
            filter.PageSize,
            u => u.IdentityUser,  // Include IdentityUser for Email and IsActive filtering
            u => u.UserSubscriptions! // Include UserSubscriptions for subscription info
        );
    }

    public async Task<UserProfile?> GetUserByIdAsync(Guid id)
    {
        return await GetFirstOrDefaultAsync(
            u => u.Id == id,
            u => u.IdentityUser!,
            u => u.Avatar!,
            u => u.UserSubscriptions!
        );
    }
    

    
    private Expression<Func<UserProfile, bool>> BuildFilterPredicate(UserFilterModel filter)
    {
        // Start with basic predicate (active users only)
        Expression<Func<UserProfile, bool>> predicate = u => u.Status == Domain.Enums.EntityStatus.Active;
        
        // Search across multiple string fields (FullName, Username, Email)
        if (!string.IsNullOrEmpty(filter.SearchKeyword))
        {
            var keyword = filter.SearchKeyword.ToLower();
            predicate = predicate.CombineAnd(u => 
                u.FullName.ToLower().Contains(keyword) ||
                (u.IdentityUser != null && u.IdentityUser.UserName != null && u.IdentityUser.UserName.ToLower().Contains(keyword)) ||
                (u.IdentityUser != null && u.IdentityUser.Email != null && u.IdentityUser.Email.ToLower().Contains(keyword))
            );
        }
        
        if (filter.Gender.HasValue)
        {
            predicate = predicate.CombineAnd(u => u.Gender == filter.Gender);
        }
        
        // Filter by account status
        if (filter.IsActive.HasValue)
        {
            predicate = predicate.CombineAnd(u => u.IdentityUser != null && u.IdentityUser.IsActive == filter.IsActive.Value);
        }
        
        // Filter by subscription status
        if (filter.HasActiveSubscription.HasValue)
        {
            if (filter.HasActiveSubscription.Value)
            {
                // User has active subscription
                predicate = predicate.CombineAnd(u => u.UserSubscriptions != null && 
                    u.UserSubscriptions.Any(us => us.IsActive && us.EndDate > DateTime.UtcNow));
            }
            else
            {
                // User has no active subscription
                predicate = predicate.CombineAnd(u => u.UserSubscriptions == null || 
                    !u.UserSubscriptions.Any(us => us.IsActive && us.EndDate > DateTime.UtcNow));
            }
        }
        
        return predicate;
    }

    private Expression<Func<UserProfile, object>> GetOrderByExpression(string? sortBy)
    {
        // Default sort by first name if not specified
        if (string.IsNullOrEmpty(sortBy))
        {
            return u => u.FirstName;
        }
        
        // Return appropriate sorting expression based on property name
        return sortBy.ToLower() switch
        {
            "name" or "firstname" => u => u.FirstName,
            "email" => u => u.IdentityUser != null ? u.IdentityUser.Email ?? string.Empty : string.Empty,
            "username" => u => u.IdentityUser != null ? u.IdentityUser.UserName ?? string.Empty : string.Empty,
            "gender" => u => u.Gender,
            "createdat" => u => u.CreatedAt,
            _ => u => u.FirstName // Default to first name for unrecognized properties
        };
    }
} 