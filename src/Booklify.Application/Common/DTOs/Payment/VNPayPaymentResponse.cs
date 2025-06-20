namespace Booklify.Application.Common.DTOs.Payment;

/// <summary>
/// VNPay payment response model
/// </summary>
public class VNPayPaymentResponse
{
    /// <summary>
    /// Payment URL to redirect user
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction reference
    /// </summary>
    public string TransactionRef { get; set; } = string.Empty;
    
    /// <summary>
    /// Success flag
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if any
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// VNPay return response model
/// </summary>
public class VNPayReturnResponse
{
    /// <summary>
    /// Payment successful flag
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction ID from VNPay
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount paid
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Response code from VNPay
    /// </summary>
    public string ResponseCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Bank code used for payment
    /// </summary>
    public string BankCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime PaymentDate { get; set; }
    
    /// <summary>
    /// Transaction reference
    /// </summary>
    public string TransactionRef { get; set; } = string.Empty;
    
    /// <summary>
    /// Message describing payment result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Bank transaction number
    /// </summary>
    public string? BankTransactionNo { get; set; }
    
    /// <summary>
    /// Card type used
    /// </summary>
    public string? CardType { get; set; }
} 