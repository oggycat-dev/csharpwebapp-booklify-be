namespace Booklify.Application.Common.DTOs.Payment;

/// <summary>
/// VNPay payment request model
/// </summary>
public class VNPayPaymentRequest
{
    /// <summary>
    /// Order ID (unique for each transaction)
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount in VND (minimum 5,000 VND)
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Order description
    /// </summary>
    public string OrderDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer phone number
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer email
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Return URL after payment
    /// </summary>
    public string? ReturnUrl { get; set; }
    
    /// <summary>
    /// Payment method (optional)
    /// </summary>
    public string? BankCode { get; set; }
    
    /// <summary>
    /// Language (vn/en)
    /// </summary>
    public string Language { get; set; } = "vn";
} 