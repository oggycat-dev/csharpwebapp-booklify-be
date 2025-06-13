using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Staff;

/// <summary>
/// Mô hình lọc danh sách nhân viên
/// </summary>
public class StaffFilterModel : FilterBase
{
    /// <summary>
    /// Lọc theo mã nhân viên (chứa)
    /// </summary>
    public string? StaffCode { get; set; }
    
    /// <summary>
    /// Lọc theo tên nhân viên (chứa)
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// Lọc theo email (chứa)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Lọc theo số điện thoại (chứa)
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// Lọc theo vị trí công việc
    /// </summary>
    public StaffPosition? Position { get; set; }
    
    /// <summary>
    /// Lọc theo trạng thái tài khoản (active/inactive)
    /// </summary>
    public bool? IsActive { get; set; }
    
    public StaffFilterModel() : base()
    {
    }
    
    public StaffFilterModel(int pageNumber, int pageSize) : base(pageNumber, pageSize)
    {
    }
} 