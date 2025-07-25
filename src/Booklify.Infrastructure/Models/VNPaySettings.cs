namespace Booklify.Infrastructure.Models;

/// <summary>
/// VNPay configuration settings
/// </summary>
public class VNPaySettings
{
    /// <summary>
    /// TMN Code provided by VNPay
    /// </summary>
    public string TmnCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Hash secret key provided by VNPay
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// VNPay payment URL
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Return URL after payment
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API version
    /// </summary>
    public string Version { get; set; } = "2.1.0";
    
    /// <summary>
    /// Command type
    /// </summary>
    public string Command { get; set; } = "pay";
    
    /// <summary>
    /// Currency code (VND)
    /// </summary>
    public string CurrencyCode { get; set; } = "VND";
    
    /// <summary>
    /// Order type
    /// </summary>
    public string OrderType { get; set; } = "other";
    
    /// <summary>
    /// Time zone
    /// </summary>
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    
    /// <summary>
    /// Payment timeout in minutes
    /// </summary>
    public int TimeoutInMinutes { get; set; } = 15;
} 