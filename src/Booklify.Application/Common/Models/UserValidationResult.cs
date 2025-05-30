using Booklify.Domain.Entities.Identity;

namespace Booklify.Application.Common.Models;

/// <summary>
/// Kết quả validation chi tiết cho user
/// </summary>
public class UserValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public AppUser? User { get; set; }
} 