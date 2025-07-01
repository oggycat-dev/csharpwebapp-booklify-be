namespace Booklify.Domain.Enums;

/// <summary>
/// Enum để định nghĩa loại ghi chú
/// </summary>
public enum ChapterNoteType
{
    /// <summary>
    /// Ghi chú dạng văn bản - người dùng tự viết note
    /// </summary>
    TextNote = 1,
    
    /// <summary>
    /// Ghi chú dạng highlight - người dùng bôi đen/tô màu đoạn văn bản
    /// </summary>
    Highlight = 2
}
