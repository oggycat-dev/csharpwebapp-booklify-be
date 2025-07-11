namespace Booklify.Domain.Enums;

public enum ReadingAction
{
    START_READING,      // Bắt đầu đọc
    UPDATE_POSITION,    // Update vị trí
    PAUSE_READING,      // Tạm dừng
    END_SESSION,        // Kết thúc session
    COMPLETE_CHAPTER,   // Hoàn thành chapter
    SYNC_DATA          // Đồng bộ data
}
