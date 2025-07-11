# 📖 CFI Reading Progress Tracking Guide

## 🎯 Overview

Hệ thống Booklify hỗ trợ tracking tiến độ đọc sách EPUB một cách chính xác sử dụng **CFI (Canonical Fragment Identifier)** standard. CFI cho phép định vị chính xác vị trí đọc trong file EPUB, không phụ thuộc vào cách render của từng EPUB reader.

## 🔧 API Endpoints

### 1. **Cập nhật vị trí đọc**
```http
POST /api/reading-progress/update-position
Authorization: Bearer {token}
Content-Type: application/json

{
  "book_id": "123e4567-e89b-12d3-a456-426614174000",
  "current_cfi": "epubcfi(/6/4[chapter1]!/4/2/1:0)",
  "current_chapter_id": "456e7890-e89b-12d3-a456-426614174001",
  "session_time_minutes": 15,
  "auto_start_session": true
}
```

### 2. **Lấy tiến độ đọc hiện tại**
```http
GET /api/reading-progress/{bookId}
Authorization: Bearer {token}
```

### 3. **Đánh dấu chapter hoàn thành**
```http
POST /api/reading-progress/complete-chapter
Authorization: Bearer {token}

{
  "book_id": "123e4567-e89b-12d3-a456-426614174000",
  "chapter_id": "456e7890-e89b-12d3-a456-426614174001",
  "completion_cfi": "epubcfi(/6/4[chapter1]!/4/999)"
}
```

## 🧭 CFI Format Explanation

### CFI Structure:
```
epubcfi(/6/4[chapter1]!/4/2/1:0)
         │ │ │        │  │ │ │
         │ │ │        │  │ │ └── Character offset (0)
         │ │ │        │  │ └──── Text node index (1)
         │ │ │        │  └────── Element index (2)
         │ │ │        └───────── Fragment separator (!)
         │ │ └────────────────── Chapter identifier [chapter1]
         │ └──────────────────── Chapter spine index (4)
         └────────────────────── Root spine index (6)
```

### Ví dụ CFI values:
- `epubcfi(/6/2!/4/2)` - Đầu chapter đầu tiên
- `epubcfi(/6/4[ch01]!/4/2/1:0)` - Vị trí cụ thể trong chapter
- `epubcfi(/6/8[ch02]!/4/10/1:256)` - Character thứ 256 trong chapter 2

## 🎯 Frontend Integration

### JavaScript Example (EPUB.js):
```javascript
// Initialize EPUB reader
const book = new Epub("book.epub");
const rendition = book.renderTo("viewer", { width: "100%", height: 600 });

// Track CFI changes
rendition.on("relocated", async (location) => {
  const currentCfi = location.start.cfi;
  
  // Update progress mỗi 10 giây để tránh spam API
  clearTimeout(progressTimer);
  progressTimer = setTimeout(async () => {
    await updateReadingProgress(bookId, currentCfi);
  }, 10000);
});

// Update progress function
async function updateReadingProgress(bookId, cfi) {
  try {
    const response = await fetch('/api/reading-progress/update-position', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        book_id: bookId,
        current_cfi: cfi,
        session_time_minutes: calculateSessionTime(),
        auto_start_session: true
      })
    });
    
    if (response.ok) {
      const progress = await response.json();
      updateProgressUI(progress.data);
    }
  } catch (error) {
    console.error('Failed to update reading progress:', error);
  }
}

// Load saved progress
async function loadSavedProgress(bookId) {
  try {
    const response = await fetch(`/api/reading-progress/${bookId}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    
    if (response.ok) {
      const progress = await response.json();
      if (progress.data.current_cfi) {
        // Jump to saved position
        rendition.display(progress.data.current_cfi);
      }
    }
  } catch (error) {
    console.error('Failed to load progress:', error);
  }
}
```

### React Hook Example:
```jsx
import { useState, useEffect, useCallback } from 'react';

const useReadingProgress = (bookId, token) => {
  const [progress, setProgress] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  
  // Load progress khi component mount
  useEffect(() => {
    if (bookId) {
      loadProgress();
    }
  }, [bookId]);
  
  const loadProgress = async () => {
    setIsLoading(true);
    try {
      const response = await fetch(`/api/reading-progress/${bookId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setProgress(data.data);
      }
    } catch (error) {
      console.error('Load progress error:', error);
    } finally {
      setIsLoading(false);
    }
  };
  
  const updateProgress = useCallback(async (cfi, sessionMinutes = 0) => {
    try {
      const response = await fetch('/api/reading-progress/update-position', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          book_id: bookId,
          current_cfi: cfi,
          session_time_minutes: sessionMinutes,
          auto_start_session: true
        })
      });
      
      if (response.ok) {
        const data = await response.json();
        setProgress(data.data);
        return data.data;
      }
    } catch (error) {
      console.error('Update progress error:', error);
    }
  }, [bookId, token]);
  
  const completeChapter = async (chapterId, completionCfi) => {
    try {
      const response = await fetch('/api/reading-progress/complete-chapter', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          book_id: bookId,
          chapter_id: chapterId,
          completion_cfi: completionCfi
        })
      });
      
      if (response.ok) {
        const data = await response.json();
        setProgress(data.data);
      }
    } catch (error) {
      console.error('Complete chapter error:', error);
    }
  };
  
  return {
    progress,
    isLoading,
    updateProgress,
    completeChapter,
    refreshProgress: loadProgress
  };
};

export default useReadingProgress;
```

## 📊 Progress Calculation

Hệ thống tính toán progress theo 3 cách:

### 1. **CFI Progress** (60% weight)
- Dựa trên vị trí CFI hiện tại trong spine của EPUB
- Chính xác theo vị trí thực tế trong file

### 2. **Chapter Completion** (40% weight)  
- Dựa trên số chapters đã hoàn thành
- User tự đánh dấu chapter complete

### 3. **Overall Progress**
- Kết hợp weighted của CFI và Chapter completion
- Formula: `(CFI_Progress * 0.6) + (Chapter_Progress * 0.4)`

## 🔄 Best Practices

### 1. **Throttle Updates**
```javascript
// Chỉ update progress mỗi 10-15 giây
const PROGRESS_UPDATE_INTERVAL = 10000;

let progressTimer;
rendition.on("relocated", (location) => {
  clearTimeout(progressTimer);
  progressTimer = setTimeout(() => {
    updateProgress(location.start.cfi);
  }, PROGRESS_UPDATE_INTERVAL);
});
```

### 2. **Handle Offline**
```javascript
// Store progress locally khi offline
const storeProgressOffline = (bookId, cfi) => {
  const offlineProgress = {
    bookId,
    cfi,
    timestamp: Date.now()
  };
  localStorage.setItem(`progress_${bookId}`, JSON.stringify(offlineProgress));
};

// Sync khi online
window.addEventListener('online', () => {
  syncOfflineProgress();
});
```

### 3. **Session Management**
```javascript
// Auto start session khi mở sách
await fetch('/api/reading-progress/start-session', {
  method: 'POST',
  body: JSON.stringify({ book_id: bookId, starting_cfi: currentCfi })
});

// Auto end session khi close hoặc idle
window.addEventListener('beforeunload', () => {
  fetch('/api/reading-progress/end-session', {
    method: 'POST',
    body: JSON.stringify({ book_id: bookId, ending_cfi: currentCfi })
  });
});
```

## 🛠️ Implementation Status

✅ **Completed:**
- Entity và Database schema
- ReadingProgressService với CFI logic
- API Controller template
- DTOs và interfaces

🚧 **TODO:**
- Implement Command/Query handlers (MediatR)
- Repository implementation  
- Business logic validation
- Unit tests

## 🎮 Testing CFI

### Manual Testing:
```javascript
// Test CFI validation
const testCfis = [
  "epubcfi(/6/2!/4/2)",           // Valid - simple
  "epubcfi(/6/4[ch01]!/4/2/1:0)", // Valid - with identifier  
  "invalid-cfi",                   // Invalid
  ""                              // Invalid - empty
];

testCfis.forEach(cfi => {
  console.log(`${cfi}: ${isValidCfi(cfi)}`);
});
```

## 📚 Resources

- [EPUB CFI Specification](http://idpf.org/epub/linking/cfi/)
- [EPUB.js Documentation](https://github.com/futurepress/epub.js/)
- [VersOne.Epub Library](https://github.com/vers-one/EpubReader)

---

**Kết luận**: Hệ thống đã sẵn sàng để implement CFI-based progress tracking. Frontend chỉ cần gửi CFI từ EPUB reader, backend sẽ tự động tính toán và lưu trữ tiến độ một cách chính xác. 