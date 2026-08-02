using System.Text.Json;
using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Exceptions;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.API.Common.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<object?> response = exception switch
        {
            AppValidationException vex  => HandleValidation(context, vex),
            NotFoundException nfex      => HandleAppException(context, nfex),
            BusinessRuleException brex  => HandleAppException(context, brex),
            ConflictException cfex      => HandleAppException(context, cfex),
            UnauthorizedException uex   => HandleAppException(context, uex),
            ForbiddenException fex      => HandleAppException(context, fex),
            AppException appEx          => HandleAppException(context, appEx),
            _                           => HandleUnexpected(context, exception)
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private ApiResponse<object?> HandleValidation(HttpContext context, AppValidationException ex)
    {
        context.Response.StatusCode = 400;
        _logger.LogWarning("Validation error: {Message}", ex.Message);
        return ApiResponse<object?>.Fail(400, ex.Message, ex.Errors);
    }

    private ApiResponse<object?> HandleAppException(HttpContext context, AppException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        _logger.LogWarning("Application exception [{StatusCode}]: {Message}", ex.StatusCode, ex.Message);
        return ApiResponse<object?>.Fail(ex.StatusCode, ex.Message);
    }

    private ApiResponse<object?> HandleUnexpected(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = 500;
        _logger.LogError(ex, "Unexpected error: {Message}", ex.Message);

        var message = _env.IsDevelopment()
            ? ex.ToString()
            : "An unexpected error occurred. Please try again later.";

        return ApiResponse<object?>.Fail(500, message);
    }
}
