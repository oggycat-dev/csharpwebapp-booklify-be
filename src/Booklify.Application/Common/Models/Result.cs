using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Booklify.Application.Common.Models;

/// <summary>
/// Error codes for API responses
/// </summary>
public enum ErrorCode
{
    // Success
    Success = 0,
    
    // Authentication & Authorization (401, 403)
    Unauthorized = 1001,
    Forbidden = 1002,
    InvalidCredentials = 1003,
    TokenExpired = 1004,
    InvalidToken = 1005,
    
    // Validation & Bad Request (400)
    ValidationFailed = 2001,
    InvalidInput = 2002,
    DuplicateEntry = 2003,
    InvalidOperation = 2004,
    
    // Not Found (404)
    NotFound = 3001,
    UserNotFound = 3002,
    BookNotFound = 3003,
    ProfileNotFound = 3004,
    ResourceNotFound = 3005,
    
    // Business Logic Errors (422)
    BusinessRuleViolation = 4001,
    InsufficientPermissions = 4002,
    ResourceConflict = 4003,
    
    // Internal Server Errors (500)
    InternalError = 5001,
    DatabaseError = 5002,
    ExternalServiceError = 5003,
    
    // File & Storage Errors
    FileUploadFailed = 6001,
    FileNotFound = 6002,
    StorageError = 6003,
    InvalidFileType = 6004,
    FileSizeTooLarge = 6005,
    
    // AI & External Service Errors
    FeatureDisabled = 7001,
    InvalidResponse = 7002,
    
    // Email Errors
    EmailSendFailed = 8001,
    EmailNotConfirmed = 8002,
    EmailAlreadyConfirmed = 8003,
    InvalidEmailToken = 8004
}

/// <summary>
/// Extension methods for ErrorCode
/// </summary>
public static class ErrorCodeExtensions
{
    /// <summary>
    /// Maps ErrorCode to HTTP status code
    /// </summary>
    public static int ToHttpStatusCode(this ErrorCode errorCode)
    {
        return errorCode switch
        {
            ErrorCode.Success => StatusCodes.Status200OK,
            
            // 400 Bad Request
            ErrorCode.ValidationFailed => StatusCodes.Status400BadRequest,
            ErrorCode.InvalidInput => StatusCodes.Status400BadRequest,
            ErrorCode.DuplicateEntry => StatusCodes.Status400BadRequest,
            ErrorCode.InvalidOperation => StatusCodes.Status400BadRequest,
            ErrorCode.InvalidFileType => StatusCodes.Status400BadRequest,
            ErrorCode.FileSizeTooLarge => StatusCodes.Status400BadRequest,
            
            // 401 Unauthorized
            ErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCode.InvalidCredentials => StatusCodes.Status401Unauthorized,
            ErrorCode.TokenExpired => StatusCodes.Status401Unauthorized,
            ErrorCode.InvalidToken => StatusCodes.Status401Unauthorized,
            
            // 403 Forbidden
            ErrorCode.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCode.InsufficientPermissions => StatusCodes.Status403Forbidden,
            
            // 404 Not Found
            ErrorCode.NotFound => StatusCodes.Status404NotFound,
            ErrorCode.UserNotFound => StatusCodes.Status404NotFound,
            ErrorCode.BookNotFound => StatusCodes.Status404NotFound,
            ErrorCode.ProfileNotFound => StatusCodes.Status404NotFound,
            ErrorCode.ResourceNotFound => StatusCodes.Status404NotFound,
            ErrorCode.FileNotFound => StatusCodes.Status404NotFound,
            
            // 422 Unprocessable Entity
            ErrorCode.BusinessRuleViolation => StatusCodes.Status422UnprocessableEntity,
            ErrorCode.ResourceConflict => StatusCodes.Status422UnprocessableEntity,
            
            // 500 Internal Server Error
            ErrorCode.InternalError => StatusCodes.Status500InternalServerError,
            ErrorCode.DatabaseError => StatusCodes.Status500InternalServerError,
            ErrorCode.ExternalServiceError => StatusCodes.Status500InternalServerError,
            ErrorCode.FileUploadFailed => StatusCodes.Status500InternalServerError,
            ErrorCode.StorageError => StatusCodes.Status500InternalServerError,
            ErrorCode.FeatureDisabled => StatusCodes.Status503ServiceUnavailable,
            ErrorCode.InvalidResponse => StatusCodes.Status502BadGateway,
            
            // Email Errors
            ErrorCode.EmailSendFailed => StatusCodes.Status500InternalServerError,
            ErrorCode.EmailNotConfirmed => StatusCodes.Status403Forbidden,
            ErrorCode.EmailAlreadyConfirmed => StatusCodes.Status400BadRequest,
            ErrorCode.InvalidEmailToken => StatusCodes.Status400BadRequest,
            
            _ => StatusCodes.Status500InternalServerError
        };
    }
    
    /// <summary>
    /// Maps nullable ErrorCode to HTTP status code, returns 200 for null
    /// </summary>
    public static int ToHttpStatusCode(this ErrorCode? errorCode)
    {
        return errorCode?.ToHttpStatusCode() ?? StatusCodes.Status200OK;
    }
}

/// <summary>
/// Generic Result pattern implementation for API responses
/// </summary>
/// <typeparam name="T">Type of data returned by the operation</typeparam>
public class Result<T>
{
    [JsonPropertyName("result")]
    public string Status { get; private set; }
    
    [JsonPropertyName("data")]
    public T? Data { get; private set; }
    
    [JsonIgnore]
    public bool IsSuccess { get; private set; }
    
    [JsonPropertyName("message")]
    public string Message { get; private set; } = string.Empty;
    
    [JsonIgnore]
    public ErrorCode? ErrorCode { get; private set; } = null;
    
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; private set; } = null;
    
    private Result(bool isSuccess, T? data, string message, ErrorCode? errorCode = null, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Status = isSuccess ? "success" : "error";
        Data = data;
        Message = message;
        ErrorCode = isSuccess ? null : (errorCode ?? Common.Models.ErrorCode.InternalError);
        Errors = isSuccess ? null : (errors?.Any() == true ? errors : null);
    }
    
    public static Result<T> Success(T data, string message = "Operation completed successfully")
    {
        return new Result<T>(true, data, message);
    }
    
    public static Result<T> Failure(string message, ErrorCode errorCode = Common.Models.ErrorCode.InternalError, List<string>? errors = null)
    {
        return new Result<T>(false, default, message, errorCode, errors);
    }
    
    public static Result<T> Failure(string message, ErrorCode errorCode, string error)
    {
        return new Result<T>(false, default, message, errorCode, new List<string> { error });
    }
}

/// <summary>
/// Non-generic Result for operations that don't return data
/// </summary>
public class Result
{
    [JsonPropertyName("result")]
    public string Status { get; private set; }
    
    [JsonIgnore]
    public bool IsSuccess { get; private set; }
    
    [JsonPropertyName("message")]
    public string Message { get; private set; } = string.Empty;
    
    [JsonIgnore]
    public ErrorCode? ErrorCode { get; private set; } = null;
    
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; private set; } = null;
    
    private Result(bool isSuccess, string message, ErrorCode? errorCode = null, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Status = isSuccess ? "success" : "error";
        Message = message;
        ErrorCode = isSuccess ? null : (errorCode ?? Common.Models.ErrorCode.InternalError);
        Errors = isSuccess ? null : (errors?.Any() == true ? errors : null);
    }
    
    public static Result Success(string message = "Operation completed successfully")
    {
        return new Result(true, message);
    }
    
    public static Result Failure(string message, ErrorCode errorCode = Common.Models.ErrorCode.InternalError, List<string>? errors = null)
    {
        return new Result(false, message, errorCode, errors);
    }
    
    public static Result Failure(string message, ErrorCode errorCode, string error)
    {
        return new Result(false, message, errorCode, new List<string> { error });
    }
}

/// <summary>
/// Extension methods for Result classes
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Gets HTTP status code from Result
    /// </summary>
    public static int GetHttpStatusCode<T>(this Result<T> result)
    {
        return result.IsSuccess ? StatusCodes.Status200OK : result.ErrorCode.ToHttpStatusCode();
    }
    
    /// <summary>
    /// Gets HTTP status code from Result
    /// </summary>
    public static int GetHttpStatusCode(this Result result)
    {
        return result.IsSuccess ? StatusCodes.Status200OK : result.ErrorCode.ToHttpStatusCode();
    }
} 