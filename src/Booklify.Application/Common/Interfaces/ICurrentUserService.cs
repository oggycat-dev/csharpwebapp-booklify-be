using Booklify.Application.Common.Models;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Service to get information about the current user
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ID of the current user
    /// </summary>
    string? UserId { get; }
    
    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Roles of the current user
    /// </summary>
    IEnumerable<string> Roles { get; }
    
    /// <summary>
    /// Kiểm tra nhanh user có hợp lệ không
    /// </summary>
    Task<bool> IsUserValidAsync();

    /// <summary>
    /// Validate chi tiết tình trạng user
    /// </summary>
    Task<UserValidationResult> ValidateUserStatusAsync();
} 