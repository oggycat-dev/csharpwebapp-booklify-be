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
    /// Người phụ trách quản lý tài khoản người dùng
    /// </summary>
    UserManager = 3,

    /// <summary>
    /// Quản lý thông tin thư viện, lịch hoạt động, tài nguyên dùng chung
    /// </summary>
    LibraryManager = 4,

    /// <summary>
    /// Nhân viên hỗ trợ kỹ thuật (fix lỗi, kiểm tra hệ thống)
    /// </summary>
    TechnicalSupport = 5,

    /// <summary>
    /// Nhân viên nhập liệu sách, metadata, tagging
    /// </summary>
    DataEntryClerk = 6,

    /// <summary>
    /// Quản lý phản hồi người dùng, đánh giá sách, báo cáo sai phạm
    /// </summary>
    CommunityModerator = 7,

    /// <summary>
    /// Nhân viên hỗ trợ AI/ML team (nếu có tra từ, tóm tắt, TTS, v.v.)
    /// </summary>
    AIAssistantManager = 8
} 