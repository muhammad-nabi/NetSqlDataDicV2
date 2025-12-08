namespace NetSqlDataDicV2.Web.Models;

/// <summary>
/// Generic result type for operations that can fail.
/// </summary>
public class OperationResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public Dictionary<string, object>? Details { get; init; }

    public static OperationResult Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static OperationResult Fail(string message, string? errorCode = null) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };

    public static OperationResult Fail(string message, Dictionary<string, object> details) =>
        new() { Success = false, Message = message, Details = details };
}

/// <summary>
/// Generic result type with data payload.
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Data { get; init; }

    public static OperationResult<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public new static OperationResult<T> Fail(string message, string? errorCode = null) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };
}
