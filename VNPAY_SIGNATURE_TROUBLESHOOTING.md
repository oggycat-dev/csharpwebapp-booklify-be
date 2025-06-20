# VNPAY SIGNATURE TROUBLESHOOTING GUIDE

## Lỗi Code 70: "Sai chữ ký" (Invalid Signature)

### Nguyên nhân chính của lỗi:

1. **Sai thuật toán hash**: VNPay yêu cầu HMAC-SHA256, không phải HMAC-SHA512
2. **Thứ tự tham số sai**: Các tham số phải được sắp xếp theo thứ tự alphabet
3. **URL encoding trong hash data**: Hash data phải sử dụng giá trị gốc, không URL encode
4. **Bao gồm vnp_SecureHash trong tính toán**: vnp_SecureHash phải được thêm AFTER tính toán hash

### Cách khắc phục đã áp dụng:

#### 1. Sử dụng HMAC-SHA256
```csharp
// ✅ ĐÚNG
using var hmac = new HMACSHA256(key);

// ❌ SAI
using var hmac = new HMACSHA512(key);
```

#### 2. Sắp xếp tham số theo alphabet
```csharp
// ✅ ĐÚNG - Sử dụng SortedDictionary
var vnpParams = new SortedDictionary<string, string>();
```

#### 3. Tách biệt hash data và URL query string
```csharp
// ✅ ĐÚNG - Hash data không URL encode
var hashData = string.Join("&", vnpParams.Select(x => $"{x.Key}={x.Value}"));
var secureHash = CreateSecureHash(hashData);

// URL query string có URL encode
var queryString = string.Join("&", vnpParams.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
queryString += $"&vnp_SecureHash={secureHash}";
```

#### 4. Loại bỏ vnp_SecureHash khỏi tính toán
```csharp
// ✅ ĐÚNG - Loại bỏ vnp_SecureHash và vnp_SecureHashType
var sortedParams = new SortedDictionary<string, string>();
foreach (var param in queryString)
{
    if (param.Key != "vnp_SecureHash" && param.Key != "vnp_SecureHashType")
    {
        sortedParams.Add(param.Key, HttpUtility.UrlDecode(param.Value));
    }
}
```

### Debug và kiểm tra:

#### Sử dụng debug endpoint:
```bash
POST /api/payment/vnpay/debug-signature
Content-Type: application/json

{
    "orderDescription": "Test payment",
    "amount": 10000,
    "language": "vn"
}
```

#### Kiểm tra hash data format:
Hash data phải có dạng:
```
vnp_Amount=1000000&vnp_Command=pay&vnp_CreateDate=20241210123456&vnp_CurrCode=VND&vnp_ExpireDate=20241210133456&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Test payment&vnp_OrderType=other&vnp_ReturnUrl=https://localhost:5123/api/payment/vnpay/return&vnp_TmnCode=2QXUI4J4&vnp_TxnRef=638676123456789012&vnp_Version=2.1.0
```

### Thông số VNPay Sandbox:
```
vnp_TmnCode: 2QXUI4J4
vnp_HashSecret: SECRETKEY123456789
vnp_Url: https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
vnp_ReturnUrl: https://localhost:5123/api/payment/vnpay/return
```

### Test Case thành công:

#### Request:
```json
{
    "orderDescription": "Thanh toan don hang 123",
    "amount": 50000,
    "language": "vn",
    "orderId": "ORDER123"
}
```

#### Expected Hash Data (ví dụ):
```
vnp_Amount=5000000&vnp_Command=pay&vnp_CreateDate=20241210143022&vnp_CurrCode=VND&vnp_ExpireDate=20241210153022&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Thanh toan don hang 123&vnp_OrderType=other&vnp_ReturnUrl=https://localhost:5123/api/payment/vnpay/return&vnp_TmnCode=2QXUI4J4&vnp_TxnRef=638676147822984567&vnp_Version=2.1.0
```

### Lưu ý quan trọng:

1. **Không bao giờ URL encode trong hash data**
2. **Luôn sắp xếp parameters theo alphabet**
3. **Sử dụng HMAC-SHA256, không phải SHA512**
4. **vnp_SecureHash không được bao gồm trong tính toán hash**
5. **Kiểm tra encoding UTF-8 cho cả key và message**

### Nếu vẫn lỗi:

1. Kiểm tra lại vnp_HashSecret: `SECRETKEY123456789`
2. Kiểm tra lại vnp_TmnCode: `2QXUI4J4`
3. Sử dụng debug endpoint để xem hash data thực tế
4. So sánh với hash data mẫu ở trên
5. Kiểm tra thời gian tạo (vnp_CreateDate) có hợp lệ không

### Code thực tế đã sửa:

Xem file `src/Booklify.Infrastructure/Services/VNPayService.cs` - method `CreatePaymentUrlAsync` và `VerifySignature` đã được cập nhật với tất cả các fix trên. 