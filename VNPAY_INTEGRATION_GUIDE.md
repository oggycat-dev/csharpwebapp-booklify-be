# VNPay Payment Integration Guide

## 🎉 Hoàn thành tích hợp VNPay

Hệ thống đã được tích hợp đầy đủ VNPay payment gateway với đầy đủ chức năng cần thiết.

## ✅ Đã implement:

### 1. **Models & DTOs**
- `VNPayPaymentRequest` - Request tạo thanh toán
- `VNPayPaymentResponse` - Response chứa URL thanh toán
- `VNPayReturnResponse` - Response xử lý kết quả thanh toán

### 2. **Service Layer**
- `IVNPayService` - Interface cho VNPay operations
- `VNPayService` - Implementation đầy đủ với:
  - Tạo payment URL với signature
  - Xử lý return response
  - Verify signature HMAC-SHA512
  - Mapping response codes

### 3. **API Controller**
- `PaymentController` với các endpoints:
  - `POST /api/payment/vnpay/create-payment` - Tạo thanh toán
  - `GET /api/payment/vnpay/return` - Xử lý return từ VNPay
  - `GET /api/payment/vnpay/banks` - Danh sách ngân hàng

### 4. **Configuration**
- `VNPaySettings` - Configuration model
- Environment variable mapping
- Dependency injection setup

## 🚀 Cách sử dụng:

### **Tạo thanh toán:**

```http
POST /api/payment/vnpay/create-payment
Content-Type: application/json

{
  "orderId": "ORDER123",
  "amount": 100000,
  "orderDescription": "Thanh toán đơn hàng ORDER123",
  "customerName": "Nguyễn Văn A",
  "customerPhone": "0987654321",
  "customerEmail": "customer@example.com",
  "language": "vn"
}
```

**Response:**
```json
{
  "success": true,
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
  "orderId": "ORDER123",
  "transactionRef": "638123456789"
}
```

### **Redirect user to payment:**
Sau khi nhận response, redirect user đến `paymentUrl` để thực hiện thanh toán.

### **Xử lý kết quả:**
VNPay sẽ redirect về URL: `/api/payment/vnpay/return` với kết quả thanh toán.

## 🔧 Configuration đã setup:

### **Sandbox Settings (Sẵn sàng test):**
```bash
VNPAY_TMN_CODE=2QXUI4J4
VNPAY_HASH_SECRET=SECRETKEY123456789
VNPAY_PAYMENT_URL=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
VNPAY_RETURN_URL=https://localhost:5123/api/payment/vnpay/return
```

## 🧪 Test với Sandbox:

### **1. Chạy application:**
```bash
dotnet run --project src/Booklify.API
```

### **2. Test API tạo payment:**
```bash
curl -X POST "https://localhost:5123/api/payment/vnpay/create-payment" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST001",
    "amount": 50000,
    "orderDescription": "Test payment",
    "customerName": "Test User",
    "customerPhone": "0123456789",
    "customerEmail": "test@example.com"
  }'
```

### **3. Sử dụng thẻ test:**
**Ngân hàng NCB:**
- Số thẻ: `9704198526191432198`
- Tên chủ thẻ: `NGUYEN VAN A`
- Ngày hết hạn: `07/15`
- Mật khẩu OTP: `123456`

**Ngân hàng Vietcombank:**
- Số thẻ: `9704061742951567672`
- Tên chủ thẻ: `NGUYEN VAN A`
- Ngày hết hạn: `07/15`
- Mật khẩu OTP: `123456`

## 🔒 Security Features:

- ✅ **HMAC-SHA512 signature** verification
- ✅ **Request validation** (amount, required fields)
- ✅ **IP address capture** cho fraud detection
- ✅ **Timeout handling** (15 phút default)
- ✅ **Error handling** comprehensive

## 📋 Response Codes:

| Code | Meaning |
|------|---------|
| 00 | Giao dịch thành công |
| 07 | Trừ tiền thành công (nghi ngờ gian lận) |
| 09 | Chưa đăng ký InternetBanking |
| 10 | Xác thực thông tin sai > 3 lần |
| 11 | Hết hạn chờ thanh toán |
| 12 | Thẻ bị khóa |
| 24 | Khách hàng hủy giao dịch |
| 51 | Tài khoản không đủ số dư |
| 65 | Vượt hạn mức giao dịch |
| 75 | Ngân hàng bảo trì |

## 🚦 Next Steps:

### **Cho Production:**
1. Đăng ký tài khoản VNPay thật tại: https://vnpay.vn/
2. Lấy TMN Code và Hash Secret thật
3. Cập nhật environment variables:
   ```bash
   VNPAY_TMN_CODE=YOUR_REAL_TMN_CODE
   VNPAY_HASH_SECRET=YOUR_REAL_HASH_SECRET
   VNPAY_PAYMENT_URL=https://pay.vnpay.vn/vpcpay.html
   ```

### **Tích hợp với Business Logic:**
1. **Lưu transaction** vào database trước khi redirect
2. **Update order status** sau khi nhận kết quả thanh toán
3. **Send notifications** cho customer
4. **Handle webhooks/IPN** cho reliability

### **Monitoring & Logging:**
- All payment requests được log
- Response codes được track
- Error handling comprehensive

## 📁 File Structure:

```
src/
├── Booklify.Application/
│   └── Common/
│       ├── DTOs/Payment/
│       │   ├── VNPayPaymentRequest.cs
│       │   └── VNPayPaymentResponse.cs
│       └── Interfaces/
│           └── IVNPayService.cs
├── Booklify.Infrastructure/
│   ├── Models/
│   │   └── VNPaySettings.cs
│   └── Services/
│       └── VNPayService.cs
├── Booklify.API/
│   ├── Controllers/
│   │   └── PaymentController.cs
│   └── Configurations/
│       └── EnvironmentConfiguration.cs (updated)
└── Documentation/
    ├── vnpay-example.env
    └── VNPAY_INTEGRATION_GUIDE.md
```

## 🎯 VNPay Integration hoàn tất! Sẵn sàng test và deploy! 🚀

**Sandbox đã setup sẵn - Chỉ cần run và test!** 