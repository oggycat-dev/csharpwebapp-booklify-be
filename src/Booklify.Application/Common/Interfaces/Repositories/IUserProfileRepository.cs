using Booklify.Domain.Entities;
using Booklify.Application.Common.DTOs.User;

namespace Booklify.Application.Common.Interfaces.Repositories;

public interface IUserProfileRepository : IGenericRepository<UserProfile>
{
    /// <summary>
    /// Get paged users with filtering and sorting
    /// </summary>
    Task<(List<UserProfile> Users, int TotalCount)> GetPagedUsersAsync(UserFilterModel filter);
    
    /// <summary>
    /// Get user by ID with all related data including subscription and avatar
    /// </summary>
    Task<UserProfile?> GetUserByIdAsync(Guid id);
} 