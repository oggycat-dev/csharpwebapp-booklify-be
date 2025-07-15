using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.User;

/// <summary>
/// Mô hình lọc danh sách người dùng
/// </summary>
public class UserFilterModel : FilterBase
{
    /// <summary>
    /// Lọc theo giới tính
    /// </summary>
    public Gender? Gender { get; set; }
    
    /// <summary>
    /// Lọc theo trạng thái tài khoản (active/inactive)
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Lọc theo trạng thái subscription (có subscription active hay không)
    /// </summary>
    public bool? HasActiveSubscription { get; set; }
    
    public UserFilterModel() : base()
    {
    }
    
    public UserFilterModel(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
} 