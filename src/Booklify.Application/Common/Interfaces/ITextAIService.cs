using Booklify.Application.Common.DTOs.BookAI;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// Interface for text AI processing services
/// </summary>
public interface ITextAIService
{
    /// <summary>
    /// Tóm tắt nội dung chương bằng tiếng Việt
    /// </summary>
    /// <param name="chapterText">Nội dung chương</param>
    /// <returns>Tóm tắt</returns>
    Task<string> SummarizeAsync(string chapterText);

    /// <summary>
    /// Dịch nội dung chương sang tiếng Việt
    /// </summary>
    /// <param name="chapterText">Nội dung chương</param>
    /// <returns>Bản dịch tiếng Việt</returns>
    Task<string> TranslateAsync(string chapterText);

    /// <summary>
    /// Trích xuất từ vựng quan trọng từ chương
    /// </summary>
    /// <param name="chapterText">Nội dung chương</param>
    /// <returns>Danh sách từ vựng</returns>
    Task<List<string>> ExtractKeywordsAsync(string chapterText);

    /// <summary>
    /// Tạo flashcard từ nội dung chương
    /// </summary>
    /// <param name="chapterText">Nội dung chương</param>
    /// <returns>Danh sách flashcard</returns>
    Task<List<FlashcardDto>> GenerateFlashcardsAsync(string chapterText);
} 