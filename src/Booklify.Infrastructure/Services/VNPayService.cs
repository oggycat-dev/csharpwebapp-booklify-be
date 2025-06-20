using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.DTOs.Payment;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// VNPay payment service implementation
/// </summary>
public class VNPayService : IVNPayService
{
    private readonly VNPaySettings _vnPaySettings;

    public VNPayService(IOptions<VNPaySettings> vnPaySettings)
    {
        _vnPaySettings = vnPaySettings.Value;
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

            // Use SortedDictionary to ensure alphabetical order
            var vnpParams = new SortedDictionary<string, string>();
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var txnRef = GenerateTransactionRef();

            // Add required parameters
            vnpParams.Add("vnp_Version", _vnPaySettings.Version);
            vnpParams.Add("vnp_Command", _vnPaySettings.Command);
            vnpParams.Add("vnp_TmnCode", _vnPaySettings.TmnCode);
            vnpParams.Add("vnp_Amount", ((long)(request.Amount * 100)).ToString()); // VNPay requires amount in cents
            vnpParams.Add("vnp_CurrCode", _vnPaySettings.CurrencyCode);
            vnpParams.Add("vnp_TxnRef", txnRef);
            vnpParams.Add("vnp_OrderInfo", request.OrderDescription);
            vnpParams.Add("vnp_OrderType", _vnPaySettings.OrderType);
            vnpParams.Add("vnp_Locale", request.Language);
            vnpParams.Add("vnp_ReturnUrl", request.ReturnUrl ?? _vnPaySettings.ReturnUrl);
            vnpParams.Add("vnp_IpAddr", ipAddress);
            vnpParams.Add("vnp_CreateDate", createDate);

            // Add expire date
            var expireDate = DateTime.Now.AddMinutes(_vnPaySettings.TimeoutInMinutes);
            vnpParams.Add("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss"));

            // Add optional parameters
            if (!string.IsNullOrEmpty(request.BankCode))
            {
                vnpParams.Add("vnp_BankCode", request.BankCode);
            }

            // Create hash data string WITHOUT URL encoding (raw values only)
            var hashData = string.Join("&", vnpParams.Select(x => $"{x.Key}={x.Value}"));

            // Create secure hash
            var secureHash = CreateSecureHash(hashData);
            
            // Create URL query string WITH URL encoding for the final URL
            var queryString = string.Join("&", vnpParams.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            queryString += $"&vnp_SecureHash={secureHash}";

            var paymentUrl = $"{_vnPaySettings.PaymentUrl}?{queryString}";

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

            // Verify signature first
            if (!VerifySignature(queryString))
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

            // Determine success based on response code
            response.Success = response.ResponseCode == "00";
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
        try
        {
            if (!queryString.ContainsKey("vnp_SecureHash"))
                return false;

            var receivedHash = queryString["vnp_SecureHash"];
            
            // Create SortedDictionary to ensure alphabetical order and exclude hash parameters
            var sortedParams = new SortedDictionary<string, string>();
            foreach (var param in queryString)
            {
                if (param.Key != "vnp_SecureHash" && param.Key != "vnp_SecureHashType")
                {
                    // URL decode the values for hash calculation
                    sortedParams.Add(param.Key, HttpUtility.UrlDecode(param.Value));
                }
            }

            // Create hash data from sorted parameters
            var hashData = string.Join("&", sortedParams.Select(x => $"{x.Key}={x.Value}"));
            var calculatedHash = CreateSecureHash(hashData);

            return calculatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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

    private string CreateSecureHash(string data)
    {
        try
        {
            var key = Encoding.UTF8.GetBytes(_vnPaySettings.HashSecret);
            var message = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(message);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating secure hash: {ex.Message}", ex);
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
} 