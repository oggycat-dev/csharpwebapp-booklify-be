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

    public PaymentController(IVNPayService vnPayService, IMediator mediator, ILogger<PaymentController> logger)
    {
        _vnPayService = vnPayService;
        _mediator = mediator;
        _logger = logger;
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
    /// Handle VNPay return URL
    /// </summary>
    /// <returns>Payment result</returns>
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
                return BadRequest(new { message = "No payment data received" });
            }

            var response = await _vnPayService.ProcessReturnResponseAsync(queryParams);

            _logger.LogInformation("VNPay return processed - OrderId: {OrderId}, Success: {Success}, ResponseCode: {ResponseCode}", 
                response.OrderId, response.Success, response.ResponseCode);

            if (response.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = response.Message,
                    orderId = response.OrderId,
                    transactionId = response.TransactionId,
                    amount = response.Amount,
                    paymentDate = response.PaymentDate,
                    bankCode = response.BankCode,
                    transactionRef = response.TransactionRef
                });
            }
            else
            {
                return Ok(new
                {
                    success = false,
                    message = response.Message,
                    orderId = response.OrderId,
                    responseCode = response.ResponseCode
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay return");
            return StatusCode(500, new { message = "Internal server error while processing payment return" });
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
} 