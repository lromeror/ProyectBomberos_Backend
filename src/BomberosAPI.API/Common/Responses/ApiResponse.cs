using System.Text.Json.Serialization;

namespace BomberosAPI.API.Common.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }

    [JsonConstructor]
    public ApiResponse() { }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true, StatusCode = 200, Message = message, Data = data
    };

    public static ApiResponse<T> Created(T data, string? message = null) => new()
    {
        Success = true, StatusCode = 201,
        Message = message ?? "Resource created successfully.", Data = data
    };

    public static ApiResponse<T> Fail(int statusCode, string message,
        IReadOnlyDictionary<string, string[]>? errors = null) => new()
    {
        Success = false, StatusCode = statusCode, Message = message, Errors = errors
    };
}

public static class ApiResponse
{
    public static ApiResponse<object?> Ok(string? message = null)
        => ApiResponse<object?>.Ok(null, message);

    public static ApiResponse<object?> Fail(int statusCode, string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        => ApiResponse<object?>.Fail(statusCode, message, errors);
}
