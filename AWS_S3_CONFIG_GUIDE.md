# Hướng dẫn cấu hình AWS S3 Storage

## ✅ Đã hoàn thành:
- ✅ Cài đặt AWS SDK (AWSSDK.S3)
- ✅ Implement S3 service code 
- ✅ Cập nhật dependency injection
- ✅ Set STORAGE_PROVIDER_TYPE = "AmazonS3"

## 🔧 Cần hoàn thành:

### 1. Tạo AWS S3 Bucket
1. Đăng nhập AWS Console: https://console.aws.amazon.com/s3/
2. Tạo bucket mới với tên unique (ví dụ: `booklify-storage-bucket`)
3. Chọn region (khuyến nghị: `us-east-1` hoặc `ap-southeast-1`)
4. **Quan trọng**: Tắt "Block all public access" để cho phép public read

### 2. Tạo IAM User và Access Keys
1. Vào AWS IAM Console: https://console.aws.amazon.com/iam/
2. Tạo user mới với tên: `booklify-s3-user`
3. Attach policy: `AmazonS3FullAccess` (hoặc tạo custom policy restrictive hơn)
4. Tạo Access Key và lưu lại:
   - Access Key ID
   - Secret Access Key

### 3. Cấu hình Environment Variables

Thay thế các giá trị sau với thông tin thật của bạn:

```powershell
# Required AWS Settings
$env:AWS_ACCESS_KEY_ID = "AKIA..."  # Thay bằng Access Key thật
$env:AWS_SECRET_ACCESS_KEY = "..."  # Thay bằng Secret Key thật
$env:AWS_S3_BUCKET_NAME = "booklify-storage-bucket"  # Tên bucket của bạn
$env:AWS_S3_REGION = "us-east-1"  # Region của bucket

# Optional Settings (có default values)
$env:AWS_S3_USE_HTTPS = "true"
$env:AWS_S3_MAX_FILE_SIZE = "52428800"  # 50MB
```

### 4. Hoặc tạo file .env
Tạo file `.env` trong root project với nội dung:

```bash
# Storage Settings
STORAGE_PROVIDER_TYPE=AmazonS3
STORAGE_BASE_URL=https://your-bucket-name.s3.us-east-1.amazonaws.com

# AWS S3 Settings
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=...
AWS_S3_BUCKET_NAME=booklify-storage-bucket
AWS_S3_REGION=us-east-1
AWS_S3_USE_HTTPS=true
AWS_S3_MAX_FILE_SIZE=52428800
```

### 5. Test S3 Configuration

Sau khi config xong, restart application và test:

```bash
# Build và run
dotnet build
dotnet run --project src/Booklify.API

# Test upload qua API
POST http://localhost:5123/api/file/upload
Content-Type: multipart/form-data
Body: [file]
```

## 🔒 Bảo mật quan trọng:

1. **Không commit credentials vào git**
2. **Sử dụng IAM policies restrictive** - chỉ cho phép operations cần thiết
3. **Consider using IAM roles** thay vì access keys trong production

## 🚨 Khắc phục sự cố:

### Lỗi "Access Denied":
- Kiểm tra Access Key/Secret Key
- Kiểm tra IAM permissions
- Kiểm tra bucket region

### Lỗi "Bucket does not exist":
- Kiểm tra tên bucket
- Kiểm tra region setting

### Lỗi "NotImplementedException":
- Đảm bảo STORAGE_PROVIDER_TYPE = "AmazonS3"
- Restart application sau khi set environment variables

## 📝 Ví dụ Custom IAM Policy (Restrictive hơn):

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:PutObject",
                "s3:DeleteObject",
                "s3:GetObjectMetadata"
            ],
            "Resource": "arn:aws:s3:::your-bucket-name/*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:ListBucket"
            ],
            "Resource": "arn:aws:s3:::your-bucket-name"
        }
    ]
}
```

## ✅ Verification Steps:

1. Check environment variables:
   ```powershell
   echo "Storage Provider: $env:STORAGE_PROVIDER_TYPE"
   echo "AWS Access Key: $env:AWS_ACCESS_KEY_ID"
   echo "S3 Bucket: $env:AWS_S3_BUCKET_NAME"
   ```

2. Test connection qua application logs

3. Upload test file qua API endpoint

Sau khi hoàn thành các bước trên, S3 storage service sẽ hoạt động bình thường! 🚀 