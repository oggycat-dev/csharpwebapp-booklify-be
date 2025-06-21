using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.DTOs.Payment;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// VNPay payment service implementation following VNPay standard library
/// </summary>
public class VNPayService : IVNPayService
{
    public const string VERSION = "2.1.0";
    private readonly VNPaySettings _vnPaySettings;

    public VNPayService(IOptions<VNPaySettings> vnPaySettings)
    {
        _vnPaySettings = vnPaySettings.Value;
        
        // Debug log settings at startup
        Console.WriteLine($"VNPay Service Initialized:");
        Console.WriteLine($"- TMN Code: {_vnPaySettings.TmnCode}");
        Console.WriteLine($"- Hash Secret: {_vnPaySettings.HashSecret?.Substring(0, 5)}***");
        Console.WriteLine($"- Payment URL: {_vnPaySettings.PaymentUrl}");
        Console.WriteLine($"- Return URL: {_vnPaySettings.ReturnUrl}");
    }

    public async Task<VNPayPaymentResponse> CreatePaymentUrlAsync(VNPayPaymentRequest request, string ipAddress)
    {
        try
        {
            // Validate input
            if (request.Amount < 5000)
            {
                return new VNPayPaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Amount must be at least 5,000 VND"
                };
            }

            // Use SortedList with VnPayCompare for proper ordering (following VNPay standard)
            var vnpParams = new SortedList<string, string>(new VnPayCompare());
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var txnRef = GenerateTransactionRef();

            // Add required parameters (following VNPay standard order)
            vnpParams.Add("vnp_Version", VERSION);
            vnpParams.Add("vnp_Command", "pay");
            vnpParams.Add("vnp_TmnCode", _vnPaySettings.TmnCode);
            vnpParams.Add("vnp_Amount", ((long)(request.Amount * 100)).ToString()); // VNPay requires amount in cents
            vnpParams.Add("vnp_CurrCode", "VND");
            vnpParams.Add("vnp_TxnRef", txnRef);
            vnpParams.Add("vnp_OrderInfo", request.OrderDescription);
            vnpParams.Add("vnp_OrderType", "other"); // default value as per VNPay standard
            vnpParams.Add("vnp_Locale", request.Language ?? "vn");
            vnpParams.Add("vnp_ReturnUrl", request.ReturnUrl ?? _vnPaySettings.ReturnUrl);
            vnpParams.Add("vnp_IpAddr", ipAddress);
            vnpParams.Add("vnp_CreateDate", createDate);

            // Add optional parameters
            if (!string.IsNullOrEmpty(request.BankCode))
            {
                vnpParams.Add("vnp_BankCode", request.BankCode);
            }

            // Create payment URL using VNPay standard method
            var paymentUrl = CreateRequestUrl(_vnPaySettings.PaymentUrl, _vnPaySettings.HashSecret, vnpParams);

            return await Task.FromResult(new VNPayPaymentResponse
            {
                Success = true,
                PaymentUrl = paymentUrl,
                OrderId = request.OrderId,
                TransactionRef = txnRef
            });
        }
        catch (Exception ex)
        {
            return new VNPayPaymentResponse
            {
                Success = false,
                ErrorMessage = $"Error creating payment URL: {ex.Message}"
            };
        }
    }

    public async Task<VNPayReturnResponse> ProcessReturnResponseAsync(Dictionary<string, string> queryString)
    {
        try
        {
            var response = new VNPayReturnResponse();

            // Verify signature first using VNPay standard method
            if (!ValidateSignature(queryString, _vnPaySettings.HashSecret))
            {
                response.Success = false;
                response.Message = "Invalid signature";
                return await Task.FromResult(response);
            }

            // Extract response data
            response.ResponseCode = queryString.GetValueOrDefault("vnp_ResponseCode", "");
            response.TransactionId = queryString.GetValueOrDefault("vnp_TransactionNo", "");
            response.TransactionRef = queryString.GetValueOrDefault("vnp_TxnRef", "");
            response.BankCode = queryString.GetValueOrDefault("vnp_BankCode", "");
            response.BankTransactionNo = queryString.GetValueOrDefault("vnp_BankTranNo", "");
            response.CardType = queryString.GetValueOrDefault("vnp_CardType", "");

            if (decimal.TryParse(queryString.GetValueOrDefault("vnp_Amount", "0"), out var amount))
            {
                response.Amount = amount / 100; // Convert from cents to VND
            }

            if (DateTime.TryParseExact(queryString.GetValueOrDefault("vnp_PayDate", ""), "yyyyMMddHHmmss", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var payDate))
            {
                response.PaymentDate = payDate;
            }

            // Check both response code and transaction status for success (VNPay standard)
            var transactionStatus = queryString.GetValueOrDefault("vnp_TransactionStatus", "");
            response.Success = response.ResponseCode == "00" && transactionStatus == "00";
            response.Message = GetPaymentStatusMessage(response.ResponseCode);

            // Extract order ID from transaction ref if needed
            response.OrderId = ExtractOrderIdFromTxnRef(response.TransactionRef);

            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            return await Task.FromResult(new VNPayReturnResponse
            {
                Success = false,
                Message = $"Error processing return response: {ex.Message}"
            });
        }
    }

    public bool VerifySignature(Dictionary<string, string> queryString)
    {
        return ValidateSignature(queryString, _vnPaySettings.HashSecret);
    }

    public string GetPaymentStatusMessage(string responseCode)
    {
        return responseCode switch
        {
            "00" => "Giao dịch thành công",
            "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
            "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
            "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
            "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
            "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
            "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.",
            "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
            "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
            "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
            "75" => "Ngân hàng thanh toán đang bảo trì.",
            "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch",
            "99" => "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)",
            _ => "Lỗi không xác định"
        };
    }

    #region VNPay Standard Library Methods

    /// <summary>
    /// Create payment URL following VNPay standard library
    /// </summary>
    private string CreateRequestUrl(string baseUrl, string hashSecret, SortedList<string, string> requestData)
    {
        var data = new StringBuilder();
        foreach (var kv in requestData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }
        
        string queryString = data.ToString();
        baseUrl += "?" + queryString;
        
        string signData = queryString;
        if (signData.Length > 0)
        {
            signData = signData.Remove(data.Length - 1, 1);
        }
        
        string vnpSecureHash = VnPayUtils.HmacSHA512(hashSecret, signData);
        baseUrl += "vnp_SecureHash=" + vnpSecureHash;
        
        return baseUrl;
    }

    /// <summary>
    /// Validate signature following VNPay standard library
    /// </summary>
    private bool ValidateSignature(Dictionary<string, string> queryString, string hashSecret)
    {
        try
        {
            if (!queryString.ContainsKey("vnp_SecureHash"))
                return false;

            var inputHash = queryString["vnp_SecureHash"];
            
            // Create SortedList with VnPayCompare for proper ordering
            var responseData = new SortedList<string, string>(new VnPayCompare());
            
            foreach (var param in queryString)
            {
                if (!string.IsNullOrEmpty(param.Key) && param.Key.StartsWith("vnp_"))
                {
                    responseData.Add(param.Key, param.Value);
                }
            }

            // Remove hash parameters
            if (responseData.ContainsKey("vnp_SecureHashType"))
            {
                responseData.Remove("vnp_SecureHashType");
            }
            if (responseData.ContainsKey("vnp_SecureHash"))
            {
                responseData.Remove("vnp_SecureHash");
            }

            // Create response data string
            var data = new StringBuilder();
            foreach (var kv in responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            
            // Remove last '&'
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            string myChecksum = VnPayUtils.HmacSHA512(hashSecret, data.ToString());
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateTransactionRef()
    {
        return DateTime.Now.Ticks.ToString();
    }

    private static string ExtractOrderIdFromTxnRef(string txnRef)
    {
        // If you encode order ID into transaction ref, extract it here
        // For now, just return the transaction ref as order ID
        return txnRef;
    }

    #endregion

    public Task<bool> ValidateCallbackAsync(Dictionary<string, string> parameters, string secureHash)
    {
        try
        {
            var isValid = ValidateSignature(parameters, _vnPaySettings.HashSecret);
            return Task.FromResult(isValid);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// VNPay utilities following VNPay standard library
/// </summary>
public static class VnPayUtils
{
    public static string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}

/// <summary>
/// VNPay comparer for proper parameter ordering following VNPay standard library
/// </summary>
public class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}
