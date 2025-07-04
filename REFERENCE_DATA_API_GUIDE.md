# Reference Data APIs Documentation

## Tổng quan
Các API này cung cấp dữ liệu tham chiếu (reference data) từ các enum trong hệ thống để sử dụng cho dropdown lists, combobox trong giao diện người dùng.

## Base URL
```
/api/common/reference
```

## Danh sách APIs

### 1. Approval Statuses (Trạng thái phê duyệt)
**Endpoint:** `GET /approval-statuses`
**Mô tả:** Lấy danh sách trạng thái phê duyệt sách

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "Pending",
      "description": "Chờ duyệt"
    },
    {
      "id": 1,
      "name": "Approved",
      "description": "Đã duyệt"
    },
    {
      "id": 2,
      "name": "Rejected",
      "description": "Từ chối"
    },
    {
      "id": 3,
      "name": "Published",
      "description": "Đã xuất bản"
    }
  ]
}
```

### 2. Entity Statuses (Trạng thái thực thể)
**Endpoint:** `GET /entity-statuses`
**Mô tả:** Lấy danh sách trạng thái chung cho các thực thể

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "Inactive",
      "description": "Không hoạt động"
    },
    {
      "id": 1,
      "name": "Active",
      "description": "Đang hoạt động"
    },
    {
      "id": 2,
      "name": "Pending",
      "description": "Chờ xử lý"
    }
  ]
}
```

### 3. Genders (Giới tính)
**Endpoint:** `GET /genders`
**Mô tả:** Lấy danh sách giới tính

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "Female",
      "description": "Nữ"
    },
    {
      "id": 1,
      "name": "Male",
      "description": "Nam"
    },
    {
      "id": 2,
      "name": "Other",
      "description": "Khác"
    }
  ]
}
```

### 4. Roles (Vai trò)
**Endpoint:** `GET /roles`
**Mô tả:** Lấy danh sách vai trò trong hệ thống

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 1,
      "name": "User",
      "description": "Người dùng"
    },
    {
      "id": 2,
      "name": "Staff",
      "description": "Nhân viên"
    },
    {
      "id": 3,
      "name": "Admin",
      "description": "Quản trị viên"
    }
  ]
}
```

### 5. Staff Positions (Vị trí nhân viên)
**Endpoint:** `GET /staff-positions`
**Mô tả:** Lấy danh sách vị trí/chức vụ nhân viên

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "Unknown",
      "description": "Không xác định"
    },
    {
      "id": 1,
      "name": "Administrator",
      "description": "Quản trị hệ thống"
    },
    {
      "id": 2,
      "name": "Staff",
      "description": "Nhân viên quản lý nội dung"
    },
    {
      "id": 3,
      "name": "UserManager",
      "description": "Quản lý tài khoản người dùng"
    },
    {
      "id": 4,
      "name": "LibraryManager",
      "description": "Quản lý thư viện"
    },
    {
      "id": 5,
      "name": "TechnicalSupport",
      "description": "Hỗ trợ kỹ thuật"
    },
    {
      "id": 6,
      "name": "DataEntryClerk",
      "description": "Nhân viên nhập liệu"
    },
    {
      "id": 7,
      "name": "CommunityModerator",
      "description": "Quản lý cộng đồng"
    },
    {
      "id": 8,
      "name": "AIAssistantManager",
      "description": "Quản lý AI/ML"
    }
  ]
}
```

### 6. Payment Statuses (Trạng thái thanh toán)
**Endpoint:** `GET /payment-statuses`
**Mô tả:** Lấy danh sách trạng thái thanh toán

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "Pending",
      "description": "Chờ thanh toán"
    },
    {
      "id": 1,
      "name": "Success",
      "description": "Thành công"
    },
    {
      "id": 2,
      "name": "Failed",
      "description": "Thất bại"
    },
    {
      "id": 3,
      "name": "Cancelled",
      "description": "Đã hủy"
    },
    {
      "id": 4,
      "name": "Refunded",
      "description": "Đã hoàn tiền"
    },
    {
      "id": 5,
      "name": "Processing",
      "description": "Đang xử lý"
    }
  ]
}
```

### 7. Chapter Note Types (Loại ghi chú chương)
**Endpoint:** `GET /chapter-note-types`
**Mô tả:** Lấy danh sách loại ghi chú của chương sách

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 1,
      "name": "TextNote",
      "description": "Ghi chú văn bản"
    },
    {
      "id": 2,
      "name": "Highlight",
      "description": "Đánh dấu văn bản"
    }
  ]
}
```

### 8. File Upload Types (Loại file upload)
**Endpoint:** `GET /file-upload-types`
**Mô tả:** Lấy danh sách loại file upload

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "None",
      "description": "Không xác định"
    },
    {
      "id": 1,
      "name": "Avatar",
      "description": "Ảnh đại diện"
    },
    {
      "id": 2,
      "name": "Document",
      "description": "Tài liệu"
    },
    {
      "id": 3,
      "name": "Image",
      "description": "Hình ảnh"
    },
    {
      "id": 4,
      "name": "Book",
      "description": "Sách"
    },
    {
      "id": 5,
      "name": "BookCover",
      "description": "Bìa sách"
    },
    {
      "id": 6,
      "name": "Epub",
      "description": "File EPUB"
    }
  ]
}
```

### 9. File Job Statuses (Trạng thái job file)
**Endpoint:** `GET /file-job-statuses`
**Mô tả:** Lấy danh sách trạng thái xử lý file

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 0,
      "name": "None",
      "description": "Không xác định"
    },
    {
      "id": 1,
      "name": "Pending",
      "description": "Chờ xử lý"
    },
    {
      "id": 2,
      "name": "Processing",
      "description": "Đang xử lý"
    },
    {
      "id": 3,
      "name": "Completed",
      "description": "Hoàn thành"
    },
    {
      "id": 4,
      "name": "Failed",
      "description": "Thất bại"
    },
    {
      "id": 5,
      "name": "Cancelled",
      "description": "Đã hủy"
    }
  ]
}
```

### 10. Book Categories (Danh mục sách)
**Endpoint:** `GET /book-categories`
**Mô tả:** Lấy danh sách danh mục sách đang hoạt động (chỉ hiển thị id và name)

**Response Example:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": "12345678-1234-5678-9abc-123456789012",
      "name": "Văn học"
    },
    {
      "id": "87654321-4321-8765-cba9-987654321098",
      "name": "Khoa học"
    },
    {
      "id": "11111111-2222-3333-4444-555555555555",
      "name": "Công nghệ"
    }
  ]
}
```

## Cập nhật mới nhất
**API Book Categories đã được thêm vào:**
- Endpoint: `GET /api/common/reference/book-categories`
- Mô tả: Lấy danh sách danh mục sách đang hoạt động (chỉ hiển thị id và name)
- Response format: Chỉ trả về id (Guid) và name (string), không có description
- Filter: Chỉ lấy các category có Status = Active và IsDeleted = false

**Tổng cộng hiện tại có 10 APIs reference data để sử dụng cho dropdown lists.**

## Sử dụng trong Frontend

### React/TypeScript Example:
```typescript
interface ReferenceDataItem {
  id: number;
  name: string;
  description: string;
}

interface ApiResponse<T> {
  isSuccess: boolean;
  data: T;
  message?: string;
}

// Fetch approval statuses
const fetchApprovalStatuses = async (): Promise<ReferenceDataItem[]> => {
  const response = await fetch('/api/common/reference/approval-statuses');
  const result: ApiResponse<ReferenceDataItem[]> = await response.json();
  return result.data;
};

// Use in dropdown component
const StatusDropdown = () => {
  const [statuses, setStatuses] = useState<ReferenceDataItem[]>([]);
  
  useEffect(() => {
    fetchApprovalStatuses().then(setStatuses);
  }, []);
  
  return (
    <select>
      {statuses.map(status => (
        <option key={status.id} value={status.id}>
          {status.description}
        </option>
      ))}
    </select>
  );
};
```

## Lưu ý
- Tất cả APIs đều là GET request và không yêu cầu authentication
- Dữ liệu được cache từ enum nên hiệu suất cao
- Response format tuân theo chuẩn Result pattern của dự án
- Các API này được nhóm trong Swagger UI dưới nhóm "Common"
