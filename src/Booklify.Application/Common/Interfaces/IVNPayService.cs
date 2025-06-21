using Booklify.Application.Common.DTOs.Payment;

namespace Booklify.Application.Common.Interfaces;

/// <summary>
/// VNPay payment service interface
/// </summary>
public interface IVNPayService
{
    /// <summary>
    /// Create payment URL for VNPay
    /// </summary>
    /// <param name="request">Payment request information</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <returns>Payment response with URL</returns>
    Task<VNPayPaymentResponse> CreatePaymentUrlAsync(VNPayPaymentRequest request, string ipAddress);
    
    /// <summary>
    /// Process VNPay return response
    /// </summary>
    /// <param name="queryString">Query string from VNPay return URL</param>
    /// <returns>Payment result</returns>
    Task<VNPayReturnResponse> ProcessReturnResponseAsync(Dictionary<string, string> queryString);
    
    /// <summary>
    /// Verify payment response signature
    /// </summary>
    /// <param name="queryString">Query string parameters</param>
    /// <returns>True if signature is valid</returns>
    bool VerifySignature(Dictionary<string, string> queryString);
    
    /// <summary>
    /// Get payment status message
    /// </summary>
    /// <param name="responseCode">VNPay response code</param>
    /// <returns>Localized message</returns>
    string GetPaymentStatusMessage(string responseCode);
    
    /// <summary>
    /// Validate VNPay callback signature
    /// </summary>
    /// <param name="parameters">Callback parameters</param>
    /// <param name="secureHash">Secure hash from VNPay</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidateCallbackAsync(Dictionary<string, string> parameters, string secureHash);
} 