using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Infrastructure.Extensions;
using Booklify.Infrastructure.Persistence;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Implementation of current user service using HttpContext
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;
    private bool? _isUserValid; // Cache kết quả
    
    // Định nghĩa các claim type chuẩn và phổ biến cho role
    private static readonly string[] ROLE_CLAIM_TYPES = new[] {
        ClaimTypes.Role, // "http://schemas.microsoft.com/ws/2005/05/identity/claims/role"
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", // Common JWT claim type 
        "role" // Simplified claim type
    };
    
    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }
    
    /// <summary>
    /// ID of the current user from claims
    /// </summary>
    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    
    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    
    /// <summary>
    /// Roles of the current user from claims
    /// </summary>
    public IEnumerable<string> Roles => GetUserRoles();
    
    /// <summary>
    /// Kiểm tra user có hợp lệ không (tồn tại và active)
    /// </summary>
    public async Task<bool> IsUserValidAsync()
    {
        // Sử dụng cache để tránh query nhiều lần trong cùng request
        if (_isUserValid.HasValue)
        {
            return _isUserValid.Value;
        }

        _isUserValid = await _dbContext.IsUserValidAsync(UserId);
        return _isUserValid.Value;
    }

    /// <summary>
    /// Lấy thông tin chi tiết về tình trạng user
    /// </summary>
    public async Task<UserValidationResult> ValidateUserStatusAsync()
    {
        return await _dbContext.ValidateUserStatusAsync(UserId);
    }
    
    /// <summary>
    /// Extract roles from claims
    /// </summary>
    private IEnumerable<string> GetUserRoles()
    {
        if (_httpContextAccessor.HttpContext?.User == null)
        {
            return Enumerable.Empty<string>();
        }
        
        var claims = _httpContextAccessor.HttpContext.User.Claims.ToList();
        
        // Lấy tất cả các role từ các định dạng claim type khác nhau
        var roles = claims
            .Where(c => ROLE_CLAIM_TYPES.Contains(c.Type))
            .Select(c => c.Value)
            .ToList();
            
        return roles;
    }
} 