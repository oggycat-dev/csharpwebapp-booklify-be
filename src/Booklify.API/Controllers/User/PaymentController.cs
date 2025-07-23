using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using MediatR;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.DTOs.Payment;
using Booklify.Application.Features.Payment.Commands.ProcessPaymentCallback;

namespace Booklify.API.Controllers.User;

/// <summary>
/// Payment management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IVNPayService _vnPayService;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentController(IVNPayService vnPayService, IMediator mediator, ILogger<PaymentController> logger, IConfiguration configuration)
    {
        _vnPayService = vnPayService;
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Create VNPay payment URL
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment URL for redirection</returns>
    [HttpPost("vnpay/create-payment")]
    public async Task<IActionResult> CreateVNPayPayment([FromBody] VNPayPaymentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get client IP address
            var ipAddress = GetClientIpAddress();

            var response = await _vnPayService.CreatePaymentUrlAsync(request, ipAddress);

            if (!response.Success)
            {
                _logger.LogWarning("VNPay payment creation failed: {ErrorMessage}", response.ErrorMessage);
                return BadRequest(new { message = response.ErrorMessage });
            }

            _logger.LogInformation("VNPay payment URL created successfully for OrderId: {OrderId}", request.OrderId);

            return Ok(new
            {
                success = true,
                paymentUrl = response.PaymentUrl,
                orderId = response.OrderId,
                transactionRef = response.TransactionRef
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VNPay payment for OrderId: {OrderId}", request.OrderId);
            return StatusCode(500, new { message = "Internal server error while creating payment" });
        }
    }

    /// <summary>
    /// Handle VNPay return URL - Process payment and show result page
    /// </summary>
    /// <returns>HTML page with payment result</returns>
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VNPayReturn()
    {
        try
        {
            // Extract query parameters
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

            if (!queryParams.Any())
            {
                _logger.LogWarning("VNPay return called with no query parameters");
                var errorHtml = GeneratePaymentErrorPage("No payment data received", GetFrontendUrl());
                return Content(errorHtml, "text/html");
            }

            // Extract VNPay parameters
            var orderId = queryParams.GetValueOrDefault("vnp_TxnRef", "");
            var vnpAmount = queryParams.GetValueOrDefault("vnp_Amount", "0");
            var vnpayTranId = queryParams.GetValueOrDefault("vnp_TransactionNo", "");
            var vnpResponseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
            var vnpTransactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "");
            var vnpSecureHash = queryParams.GetValueOrDefault("vnp_SecureHash", "");

            // Validate signature first
            bool checkSignature = _vnPayService.VerifySignature(queryParams);
            
            if (!checkSignature)
            {
                _logger.LogWarning("VNPay return - Invalid signature, InputData={0}", Request.GetDisplayUrl());
                var errorHtml = GeneratePaymentErrorPage("Invalid payment signature", GetFrontendUrl());
                return Content(errorHtml, "text/html");
            }

            // Convert amount from cents to VND
            if (!long.TryParse(vnpAmount, out var amount))
            {
                _logger.LogWarning("Invalid amount in VNPay return: {Amount}", vnpAmount);
                var errorHtml = GeneratePaymentErrorPage("Invalid payment amount", GetFrontendUrl());
                return Content(errorHtml, "text/html");
            }
            amount = amount / 100; // Convert from cents

            // Create command to process callback
            var command = new ProcessPaymentCallbackCommand
            {
                OrderId = orderId,
                TransactionId = vnpayTranId,
                ResponseCode = vnpResponseCode,
                TransactionStatus = vnpTransactionStatus,
                Amount = amount,
                PaymentMethod = queryParams.GetValueOrDefault("vnp_CardType", ""),
                BankCode = queryParams.GetValueOrDefault("vnp_BankCode", ""),
                PayDate = queryParams.GetValueOrDefault("vnp_PayDate", ""),
                SecureHash = vnpSecureHash,
                AdditionalData = queryParams
            };

            // Process the callback
            var result = await _mediator.Send(command);
            var frontendUrl = GetFrontendUrl();

            if (result.IsSuccess && result.Data?.PaymentStatus == Domain.Enums.PaymentStatus.Success)
            {
                _logger.LogInformation("VNPay return processed successfully, OrderId={0}, VNPAY TranId={1}", 
                    orderId, vnpayTranId);
                
                var successHtml = GeneratePaymentSuccessPage(result.Data, frontendUrl);
                return Content(successHtml, "text/html");
            }
            else if (result.Data?.PaymentStatus == Domain.Enums.PaymentStatus.Cancelled)
            {
                _logger.LogInformation("VNPay return - Payment cancelled, OrderId={0}", orderId);
                var cancelHtml = GeneratePaymentCancelPage(frontendUrl);
                return Content(cancelHtml, "text/html");
            }
            else
            {
                _logger.LogWarning("VNPay return - Payment failed, OrderId={0}, Error={1}", orderId, result.Message);
                var errorHtml = GeneratePaymentErrorPage(result.Message, frontendUrl);
                return Content(errorHtml, "text/html");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay return");
            var errorHtml = GeneratePaymentErrorPage("Internal server error while processing payment", GetFrontendUrl());
            return Content(errorHtml, "text/html");
        }
    }

    /// <summary>
    /// Handle VNPay IPN (Instant Payment Notification) following VNPay standard
    /// </summary>
    /// <returns>IPN response</returns>
    [HttpPost("vnpay/ipn")]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VNPayIPN()
    {
        string returnContent = string.Empty;
        
        try
        {
            _logger.LogInformation("Begin VNPay IPN, URL={0}", Request.GetDisplayUrl());

            // Extract query parameters (VNPay sends IPN via GET request)
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

            if (!queryParams.Any())
            {
                _logger.LogWarning("VNPay IPN called with no query parameters");
                returnContent = "{\"RspCode\":\"99\",\"Message\":\"Input data required\"}";
            }
            else
            {
                // Extract VNPay parameters following standard
                var orderId = queryParams.GetValueOrDefault("vnp_TxnRef", "");
                var vnpAmount = queryParams.GetValueOrDefault("vnp_Amount", "0");
                var vnpayTranId = queryParams.GetValueOrDefault("vnp_TransactionNo", "");
                var vnpResponseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
                var vnpTransactionStatus = queryParams.GetValueOrDefault("vnp_TransactionStatus", "");
                var vnpSecureHash = queryParams.GetValueOrDefault("vnp_SecureHash", "");

                // Validate signature first (following VNPay standard)
                bool checkSignature = _vnPayService.VerifySignature(queryParams);
                
                if (checkSignature)
                {
                    // Convert amount from cents to VND
                    if (!long.TryParse(vnpAmount, out var amount))
                    {
                        _logger.LogWarning("Invalid amount in VNPay IPN: {Amount}", vnpAmount);
                        returnContent = "{\"RspCode\":\"04\",\"Message\":\"Invalid amount\"}";
                    }
                    else
                    {
                        amount = amount / 100; // Convert from cents

                        // Create command to process callback
                        var command = new ProcessPaymentCallbackCommand
                        {
                            OrderId = orderId,
                            TransactionId = vnpayTranId,
                            ResponseCode = vnpResponseCode,
                            TransactionStatus = vnpTransactionStatus,
                            Amount = amount,
                            PaymentMethod = queryParams.GetValueOrDefault("vnp_CardType", ""),
                            BankCode = queryParams.GetValueOrDefault("vnp_BankCode", ""),
                            PayDate = queryParams.GetValueOrDefault("vnp_PayDate", ""),
                            SecureHash = vnpSecureHash,
                            AdditionalData = queryParams
                        };

                        // Process the callback
                        var result = await _mediator.Send(command);

                        if (result.IsSuccess)
                        {
                            _logger.LogInformation("VNPay IPN processed successfully, OrderId={0}, VNPAY TranId={1}", 
                                orderId, vnpayTranId);
                            returnContent = "{\"RspCode\":\"00\",\"Message\":\"Confirm Success\"}";
                        }
                        else if (result.Message.Contains("already confirmed"))
                        {
                            _logger.LogInformation("VNPay IPN - Order already confirmed, OrderId={0}", orderId);
                            returnContent = "{\"RspCode\":\"02\",\"Message\":\"Order already confirmed\"}";
                        }
                        else if (result.Message.Contains("not found"))
                        {
                            _logger.LogWarning("VNPay IPN - Order not found, OrderId={0}", orderId);
                            returnContent = "{\"RspCode\":\"01\",\"Message\":\"Order not found\"}";
                        }
                        else if (result.Message.Contains("amount"))
                        {
                            _logger.LogWarning("VNPay IPN - Invalid amount, OrderId={0}", orderId);
                            returnContent = "{\"RspCode\":\"04\",\"Message\":\"Invalid amount\"}";
                        }
                        else
                        {
                            _logger.LogError("VNPay IPN processing failed, OrderId={0}, Error={1}", 
                                orderId, result.Message);
                            returnContent = "{\"RspCode\":\"99\",\"Message\":\"Unknown error\"}";
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("VNPay IPN - Invalid signature, InputData={0}", Request.GetDisplayUrl());
                    returnContent = "{\"RspCode\":\"97\",\"Message\":\"Invalid signature\"}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay IPN");
            returnContent = "{\"RspCode\":\"99\",\"Message\":\"Unknown error\"}";
        }

        // Return response to VNPay (following VNPay standard)
        Response.Clear();
        Response.ContentType = "application/json";
        return Content(returnContent);
    }

    /// <summary>
    /// Verify VNPay payment status
    /// </summary>
    /// <param name="orderId">Order ID to check</param>
    /// <returns>Payment verification result</returns>
    [HttpGet("vnpay/verify/{orderId}")]
    public async Task<IActionResult> VerifyPayment(string orderId)
    {
        try
        {
            // TODO: Implement payment verification logic
            // This might involve querying your database for payment status
            // or calling VNPay query API

            _logger.LogInformation("Payment verification requested for OrderId: {OrderId}", orderId);

            return Ok(new
            {
                orderId,
                message = "Payment verification not implemented yet"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment for OrderId: {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error while verifying payment" });
        }
    }

    /// <summary>
    /// Get supported bank codes for VNPay
    /// </summary>
    /// <returns>List of supported banks</returns>
    [HttpGet("vnpay/banks")]
    public IActionResult GetSupportedBanks()
    {
        var banks = new[]
        {
            new { Code = "VNPAYQR", Name = "Cổng thanh toán VNPAYQR" },
            new { Code = "VNBANK", Name = "Ngân hàng điện tử VNBank" },
            new { Code = "INTCARD", Name = "Thẻ quốc tế" },
            new { Code = "VISA", Name = "Thẻ quốc tế Visa" },
            new { Code = "MASTERCARD", Name = "Thẻ quốc tế MasterCard" },
            new { Code = "VIETCOMBANK", Name = "Ngân hàng Vietcombank" },
            new { Code = "AGRIBANK", Name = "Ngân hàng Agribank" },
            new { Code = "SACOMBANK", Name = "Ngân hàng Sacombank" },
            new { Code = "VIETINBANK", Name = "Ngân hàng Vietinbank" },
            new { Code = "BIDV", Name = "Ngân hàng BIDV" },
            new { Code = "TECHCOMBANK", Name = "Ngân hàng Techcombank" },
            new { Code = "MBBANK", Name = "Ngân hàng MBBank" },
            new { Code = "ACB", Name = "Ngân hàng ACB" }
        };

        return Ok(new { banks });
    }

    /// <summary>
    /// Debug VNPay signature generation (for development only)
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Debug information</returns>
    [HttpPost("vnpay/debug-signature")]
    public async Task<IActionResult> DebugVNPaySignature([FromBody] VNPayPaymentRequest request)
    {
        try
        {
            // Get client IP address
            var ipAddress = GetClientIpAddress();

            // Use SortedDictionary to show debug info with correct sorting
            var vnpParams = new SortedDictionary<string, string>();
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var txnRef = DateTime.Now.Ticks.ToString();

            // Add required parameters (same as in service)
            vnpParams.Add("vnp_Version", "2.1.0");
            vnpParams.Add("vnp_Command", "pay");
            vnpParams.Add("vnp_TmnCode", "2QXUI4J4");
            vnpParams.Add("vnp_Amount", ((long)(request.Amount * 100)).ToString());
            vnpParams.Add("vnp_CurrCode", "VND");
            vnpParams.Add("vnp_TxnRef", txnRef);
            vnpParams.Add("vnp_OrderInfo", request.OrderDescription);
            vnpParams.Add("vnp_OrderType", "other");
            vnpParams.Add("vnp_Locale", request.Language);
            vnpParams.Add("vnp_ReturnUrl", "https://localhost:5123/api/payment/vnpay/return");
            vnpParams.Add("vnp_IpAddr", ipAddress);
            vnpParams.Add("vnp_CreateDate", createDate);

            var expireDate = DateTime.Now.AddMinutes(15);
            vnpParams.Add("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss"));

            // Create hash data (already sorted by SortedDictionary)
            var hashData = string.Join("&", vnpParams.Select(x => $"{x.Key}={x.Value}"));

            // Create hash manually for debug
            var hashSecret = "SECRETKEY123456789";
            var key = System.Text.Encoding.UTF8.GetBytes(hashSecret);
            var message = System.Text.Encoding.UTF8.GetBytes(hashData);

            string secureHash;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(key))
            {
                var hash = hmac.ComputeHash(message);
                secureHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            return Ok(new
            {
                success = true,
                debug = new
                {
                    hashData = hashData,
                    hashSecret = hashSecret,
                    secureHash = secureHash,
                    parameters = vnpParams.ToList(),
                    createDate = createDate,
                    txnRef = txnRef,
                    ipAddress = ipAddress
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debug signature");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Debug payment callback processing (for development only)
    /// </summary>
    /// <param name="request">Payment callback request</param>
    /// <returns>Debug processing result</returns>
    [HttpPost("debug/process-callback")]
    public async Task<IActionResult> DebugProcessCallback([FromBody] ProcessPaymentCallbackCommand request)
    {
        try
        {
            _logger.LogInformation("Debug processing payment callback for OrderId: {OrderId}", request.OrderId);

            var result = await _mediator.Send(request);

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.Message,
                data = result.Data,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debug payment callback for OrderId: {OrderId}", request.OrderId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // Check for forwarded IP (in case of proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        else if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        return ipAddress ?? "127.0.0.1";
    }

    private string GetFrontendUrl()
    {
        var frontendUrl = _configuration["FrontendUrl"];
        if (string.IsNullOrEmpty(frontendUrl))
        {
            _logger.LogWarning("FrontendUrl not configured in appsettings.json");
            return "https://localhost:5173"; // Fallback to a default if not configured
        }
        
        // Get first URL if multiple URLs are configured (comma-separated)
        var urls = frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var firstUrl = urls[0].Trim();
        
        _logger.LogInformation("Using frontend URL: {FrontendUrl}", firstUrl);
        return firstUrl;
    }

    private string GeneratePaymentErrorPage(string message, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thanh toán thất bại - Booklify</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #ff9500 0%, #ff6b00 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            color: #333;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(255, 149, 0, 0.3);
            padding: 50px 40px;
            text-align: center;
            max-width: 500px;
            width: 90%;
            position: relative;
            overflow: hidden;
        }}
        
        .container::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 5px;
            background: linear-gradient(90deg, #dc3545, #c82333, #dc3545);
        }}
        
        .error-icon {{
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, #dc3545, #c82333);
            border-radius: 50%;
            margin: 0 auto 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            animation: shake 0.6s ease-out;
        }}
        
        .error-icon::after {{
            content: '✗';
            color: white;
            font-size: 36px;
            font-weight: bold;
        }}
        
        h1 {{
            color: #dc3545;
            font-size: 2.2em;
            margin-bottom: 20px;
            font-weight: 700;
        }}
        
        .message {{
            color: #666;
            font-size: 1.1em;
            line-height: 1.6;
            margin-bottom: 20px;
        }}
        
        .error-details {{
            background: #fff5f5;
            border: 2px solid #dc3545;
            border-radius: 12px;
            padding: 20px;
            margin: 20px 0;
            color: #dc3545;
        }}
        
        .redirect-info {{
            background: #fff5f0;
            border: 2px solid #ff9500;
            border-radius: 12px;
            padding: 20px;
            margin: 30px 0;
        }}
        
        .redirect-text {{
            color: #ff6b00;
            font-weight: 600;
            margin-bottom: 10px;
        }}
        
        .countdown {{
            font-size: 2em;
            font-weight: bold;
            color: #ff6b00;
            margin: 10px 0;
        }}
        
        .home-button {{
            display: inline-block;
            background: linear-gradient(135deg, #ff9500, #ff6b00);
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 50px;
            font-weight: 600;
            font-size: 1.1em;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(255, 149, 0, 0.4);
            margin-top: 20px;
        }}
        
        .home-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 149, 0, 0.6);
        }}
        
        .booklify-logo {{
            color: #ff6b00;
            font-size: 1.5em;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }}
        
        @keyframes shake {{
            0%, 100% {{ transform: translateX(0); }}
            25% {{ transform: translateX(-5px); }}
            75% {{ transform: translateX(5px); }}
        }}
        
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        
        .container > * {{
            animation: fadeIn 0.6s ease-out forwards;
        }}
        
        .container > *:nth-child(2) {{ animation-delay: 0.1s; }}
        .container > *:nth-child(3) {{ animation-delay: 0.2s; }}
        .container > *:nth-child(4) {{ animation-delay: 0.3s; }}
        .container > *:nth-child(5) {{ animation-delay: 0.4s; }}
        .container > *:nth-child(6) {{ animation-delay: 0.5s; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='booklify-logo'>
            📚 Booklify
        </div>
        
        <div class='error-icon'></div>
        
        <h1>❌ Thanh toán thất bại</h1>
        
        <p class='message'>
            Rất tiếc, quá trình thanh toán không thành công.
        </p>
        
        <div class='error-details'>
            <strong>Lỗi:</strong> {message}
        </div>
        
        <p class='message'>
            Vui lòng thử lại hoặc liên hệ với chúng tôi để được hỗ trợ.
        </p>
        
        <div class='redirect-info'>
            <div class='redirect-text'>Tự động chuyển về trang chủ trong:</div>
            <div class='countdown' id='countdown'>4</div>
            <div style='color: #888; font-size: 0.9em;'>giây</div>
        </div>
        
        <a href='{frontendUrl}' class='home-button'>
            🏠 Về trang chủ
        </a>
    </div>

    <script>
        let countdown = 4;
        const countdownElement = document.getElementById('countdown');
        
        const timer = setInterval(() => {{
            countdown--;
            countdownElement.textContent = countdown;
            
            if (countdown <= 0) {{
                clearInterval(timer);
                window.location.href = '{frontendUrl}';
            }}
        }}, 1000);
        
        // Allow manual navigation
        document.addEventListener('click', function(e) {{
            if (e.target.classList.contains('home-button')) {{
                clearInterval(timer);
            }}
        }});
    </script>
</body>
</html>";
    }

    private string GeneratePaymentSuccessPage(Application.Common.DTOs.Subscription.PaymentStatusResponse payment, string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thanh toán thành công - Booklify</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #ff9500 0%, #ff6b00 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            color: #333;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(255, 149, 0, 0.3);
            padding: 50px 40px;
            text-align: center;
            max-width: 600px;
            width: 90%;
            position: relative;
            overflow: hidden;
        }}
        
        .container::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 5px;
            background: linear-gradient(90deg, #28a745, #20c997, #28a745);
        }}
        
        .success-icon {{
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, #28a745, #20c997);
            border-radius: 50%;
            margin: 0 auto 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            animation: bounce 0.6s ease-out;
        }}
        
        .success-icon::after {{
            content: '✓';
            color: white;
            font-size: 36px;
            font-weight: bold;
        }}
        
        h1 {{
            color: #28a745;
            font-size: 2.2em;
            margin-bottom: 20px;
            font-weight: 700;
        }}
        
        .message {{
            color: #666;
            font-size: 1.1em;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        
        .payment-details {{
            background: #f8fff9;
            border: 2px solid #28a745;
            border-radius: 12px;
            padding: 20px;
            margin: 30px 0;
            text-align: left;
        }}
        
        .payment-details h3 {{
            color: #28a745;
            margin-bottom: 15px;
            text-align: center;
        }}
        
        .detail-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
            padding: 8px 0;
            border-bottom: 1px solid #e9ecef;
        }}
        
        .detail-label {{
            font-weight: 600;
            color: #495057;
        }}
        
        .detail-value {{
            color: #28a745;
            font-weight: 500;
        }}
        
        .redirect-info {{
            background: #fff5f0;
            border: 2px solid #ff9500;
            border-radius: 12px;
            padding: 20px;
            margin: 30px 0;
        }}
        
        .redirect-text {{
            color: #ff6b00;
            font-weight: 600;
            margin-bottom: 10px;
        }}
        
        .countdown {{
            font-size: 2em;
            font-weight: bold;
            color: #ff6b00;
            margin: 10px 0;
        }}
        
        .home-button {{
            display: inline-block;
            background: linear-gradient(135deg, #ff9500, #ff6b00);
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 50px;
            font-weight: 600;
            font-size: 1.1em;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(255, 149, 0, 0.4);
        }}
        
        .home-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 149, 0, 0.6);
        }}
        
        .booklify-logo {{
            color: #ff6b00;
            font-size: 1.5em;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }}
        
        @keyframes bounce {{
            0%, 20%, 50%, 80%, 100% {{ transform: translateY(0); }}
            40% {{ transform: translateY(-10px); }}
            60% {{ transform: translateY(-5px); }}
        }}
        
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        
        .container > * {{
            animation: fadeIn 0.6s ease-out forwards;
        }}
        
        .container > *:nth-child(2) {{ animation-delay: 0.1s; }}
        .container > *:nth-child(3) {{ animation-delay: 0.2s; }}
        .container > *:nth-child(4) {{ animation-delay: 0.3s; }}
        .container > *:nth-child(5) {{ animation-delay: 0.4s; }}
        .container > *:nth-child(6) {{ animation-delay: 0.5s; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='booklify-logo'>
            📚 Booklify
        </div>
        
        <div class='success-icon'></div>
        
        <h1>🎉 Thanh toán thành công!</h1>
        
        <p class='message'>
            Chúc mừng! Thanh toán của bạn đã được xử lý thành công.<br>
            Subscription đã được kích hoạt và bạn có thể bắt đầu sử dụng dịch vụ.
        </p>
        
        <div class='payment-details'>
            <h3>Chi tiết thanh toán</h3>
            <div class='detail-row'>
                <span class='detail-label'>Số tiền:</span>
                <span class='detail-value'>{payment.Amount:N0} VND</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Mã đơn hàng:</span>
                <span class='detail-value'>{payment.PaymentId}</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Mã giao dịch:</span>
                <span class='detail-value'>{payment.TransactionId}</span>
            </div>
            <div class='detail-row'>
                <span class='detail-label'>Thời gian:</span>
                <span class='detail-value'>{payment.PaymentDate?.ToString("dd/MM/yyyy HH:mm:ss")}</span>
            </div>
        </div>
        
        <div class='redirect-info'>
            <div class='redirect-text'>Tự động chuyển về trang chủ trong:</div>
            <div class='countdown' id='countdown'>4</div>
            <div style='color: #888; font-size: 0.9em;'>giây</div>
        </div>
        
        <a href='{frontendUrl}' class='home-button'>
            🏠 Về trang chủ
        </a>
    </div>

    <script>
        let countdown = 4;
        const countdownElement = document.getElementById('countdown');
        
        const timer = setInterval(() => {{
            countdown--;
            countdownElement.textContent = countdown;
            
            if (countdown <= 0) {{
                clearInterval(timer);
                window.location.href = '{frontendUrl}';
            }}
        }}, 1000);
        
        // Allow manual navigation
        document.addEventListener('click', function(e) {{
            if (e.target.classList.contains('home-button')) {{
                clearInterval(timer);
            }}
        }});
    </script>
</body>
</html>";
    }

    private string GeneratePaymentCancelPage(string frontendUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thanh toán bị hủy - Booklify</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #ff9500 0%, #ff6b00 100%);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            color: #333;
        }}
        
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(255, 149, 0, 0.3);
            padding: 50px 40px;
            text-align: center;
            max-width: 500px;
            width: 90%;
            position: relative;
            overflow: hidden;
        }}
        
        .container::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 5px;
            background: linear-gradient(90deg, #6c757d, #5a6268, #6c757d);
        }}
        
        .cancel-icon {{
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, #6c757d, #5a6268);
            border-radius: 50%;
            margin: 0 auto 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            animation: bounce 0.6s ease-out;
        }}
        
        .cancel-icon::after {{
            content: '⏸';
            color: white;
            font-size: 36px;
            font-weight: bold;
        }}
        
        h1 {{
            color: #6c757d;
            font-size: 2.2em;
            margin-bottom: 20px;
            font-weight: 700;
        }}
        
        .message {{
            color: #666;
            font-size: 1.1em;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        
        .redirect-info {{
            background: #fff5f0;
            border: 2px solid #ff9500;
            border-radius: 12px;
            padding: 20px;
            margin: 30px 0;
        }}
        
        .redirect-text {{
            color: #ff6b00;
            font-weight: 600;
            margin-bottom: 10px;
        }}
        
        .countdown {{
            font-size: 2em;
            font-weight: bold;
            color: #ff6b00;
            margin: 10px 0;
        }}
        
        .home-button {{
            display: inline-block;
            background: linear-gradient(135deg, #ff9500, #ff6b00);
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 50px;
            font-weight: 600;
            font-size: 1.1em;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(255, 149, 0, 0.4);
        }}
        
        .home-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(255, 149, 0, 0.6);
        }}
        
        .booklify-logo {{
            color: #ff6b00;
            font-size: 1.5em;
            font-weight: bold;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }}
        
        @keyframes bounce {{
            0%, 20%, 50%, 80%, 100% {{ transform: translateY(0); }}
            40% {{ transform: translateY(-10px); }}
            60% {{ transform: translateY(-5px); }}
        }}
        
        @keyframes fadeIn {{
            from {{ opacity: 0; transform: translateY(20px); }}
            to {{ opacity: 1; transform: translateY(0); }}
        }}
        
        .container > * {{
            animation: fadeIn 0.6s ease-out forwards;
        }}
        
        .container > *:nth-child(2) {{ animation-delay: 0.1s; }}
        .container > *:nth-child(3) {{ animation-delay: 0.2s; }}
        .container > *:nth-child(4) {{ animation-delay: 0.3s; }}
        .container > *:nth-child(5) {{ animation-delay: 0.4s; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='booklify-logo'>
            📚 Booklify
        </div>
        
        <div class='cancel-icon'></div>
        
        <h1>⏸️ Thanh toán bị hủy</h1>
        
        <p class='message'>
            Bạn đã hủy quá trình thanh toán.<br>
            Đơn hàng chưa được xử lý và bạn có thể thử lại bất cứ lúc nào.
        </p>
        
        <div class='redirect-info'>
            <div class='redirect-text'>Tự động chuyển về trang chủ trong:</div>
            <div class='countdown' id='countdown'>4</div>
            <div style='color: #888; font-size: 0.9em;'>giây</div>
        </div>
        
        <a href='{frontendUrl}' class='home-button'>
            🏠 Về trang chủ
        </a>
    </div>

    <script>
        let countdown = 4;
        const countdownElement = document.getElementById('countdown');
        
        const timer = setInterval(() => {{
            countdown--;
            countdownElement.textContent = countdown;
            
            if (countdown <= 0) {{
                clearInterval(timer);
                window.location.href = '{frontendUrl}';
            }}
        }}, 1000);
        
        // Allow manual navigation
        document.addEventListener('click', function(e) {{
            if (e.target.classList.contains('home-button')) {{
                clearInterval(timer);
            }}
        }});
    </script>
</body>
</html>";
    }
} 