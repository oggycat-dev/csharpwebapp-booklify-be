using System.Text.Json.Serialization;

namespace Booklify.Application.Common.Models;

/// <summary>
/// Generic paginated result for API responses
/// </summary>
/// <typeparam name="T">Type of data returned by the operation</typeparam>
public class PaginatedResult<T>
{
    [JsonPropertyName("result")]
    public string Status { get; private set; } = "success";
    
    [JsonPropertyName("data")]
    public List<T>? Data { get; private set; }
    
    [JsonIgnore]
    public bool IsSuccess { get; private set; } = true;
    
    [JsonPropertyName("message")]
    public string Message { get; private set; } = "Data retrieved successfully";
    
    [JsonIgnore]
    public ErrorCode? ErrorCode { get; private set; } = null;
    
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; private set; } = null;
    
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; private set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; private set; }
    
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; private set; }
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; private set; }
    
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => PageNumber > 1;
    
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => PageNumber < TotalPages;

    // Success constructor
    private PaginatedResult(List<T> data, int pageNumber, int pageSize, int totalCount, string message = "Data retrieved successfully")
    {
        IsSuccess = true;
        Status = "success";
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
        Message = message;
        ErrorCode = null;
        Errors = null;
    }

    // Failure constructor
    private PaginatedResult(string message, ErrorCode errorCode, List<string>? errors = null, int pageNumber = 1, int pageSize = 10)
    {
        IsSuccess = false;
        Status = "error";
        Data = new List<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = 0;
        TotalPages = 0;
        Message = message;
        ErrorCode = errorCode;
        Errors = errors?.Any() == true ? errors : null;
    }

    // Success methods
    public static PaginatedResult<T> Success(List<T> data, int pageNumber, int pageSize, int totalCount, string message = "Data retrieved successfully")
    {
        return new PaginatedResult<T>(data, pageNumber, pageSize, totalCount, message);
    }

    // Failure methods with ErrorCode
    public static PaginatedResult<T> Failure(string message, ErrorCode errorCode, List<string>? errors, int pageNumber = 1, int pageSize = 10)
    {
        return new PaginatedResult<T>(message, errorCode, errors, pageNumber, pageSize);
    }

    public static PaginatedResult<T> Failure(string message, ErrorCode errorCode, string error, int pageNumber = 1, int pageSize = 10)
    {
        return new PaginatedResult<T>(message, errorCode, new List<string> { error }, pageNumber, pageSize);
    }

    public static PaginatedResult<T> Failure(string message, ErrorCode errorCode, int pageNumber = 1, int pageSize = 10)
    {
        return new PaginatedResult<T>(message, errorCode, null, pageNumber, pageSize);
    }

    // Legacy failure methods for backward compatibility
    public static PaginatedResult<T> Failure(string message, List<string> errors)
    {
        return new PaginatedResult<T>(message, Common.Models.ErrorCode.InternalError, errors, 1, 10);
    }

    public static PaginatedResult<T> Failure(string message, string error)
    {
        return new PaginatedResult<T>(message, Common.Models.ErrorCode.InternalError, new List<string> { error }, 1, 10);
    }

    public static PaginatedResult<T> Failure(string message)
    {
        return new PaginatedResult<T>(message, Common.Models.ErrorCode.InternalError, null, 1, 10);
    }

    /// <summary>
    /// Gets HTTP status code from PaginatedResult
    /// </summary>
    public int GetHttpStatusCode()
    {
        return IsSuccess ? 200 : ErrorCode?.ToHttpStatusCode() ?? 500;
    }
} 