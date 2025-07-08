                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     namespace Booklify.Domain.Enums;

/// <summary>
/// Represents different departments in the library
/// </summary>
public enum StaffPosition
{
    /// <summary>
    /// Unknown position
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Quản trị hệ thống tổng thể (Super Admin)
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Quản lý nội dung sách, duyệt và kiểm duyệt nội dung
    /// </summary>
    Staff = 2,

    /// <summary>
    /// Quản lý thông tin thư viện, lịch hoạt động, tài nguyên dùng chung
    /// </summary>
    LibraryManager = 3,
} 