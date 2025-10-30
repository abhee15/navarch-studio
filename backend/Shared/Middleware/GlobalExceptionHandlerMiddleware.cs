using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.DTOs;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;

namespace Shared.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions
/// and returns consistent error responses across the API.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Prevent double-processing if response has already started
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot write exception response");
            return;
        }

        var correlationId = context.Response.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        var errorResponse = exception switch
        {
            // Validation exceptions (FluentValidation)
            ValidationException validationEx => CreateValidationErrorResponse(
                context, validationEx, correlationId),

            // Not found exceptions
            KeyNotFoundException or InvalidOperationException when exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => CreateErrorResponse(
                    context,
                    HttpStatusCode.NotFound,
                    "Not Found",
                    exception.Message,
                    correlationId,
                    exception),

            // Bad request / argument exceptions
            ArgumentException or ArgumentNullException or ArgumentOutOfRangeException
                => CreateErrorResponse(
                    context,
                    HttpStatusCode.BadRequest,
                    "Bad Request",
                    exception.Message,
                    correlationId,
                    exception),

            // Unauthorized exceptions
            UnauthorizedAccessException => CreateErrorResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to perform this action",
                correlationId,
                exception),

            // Conflict exceptions (e.g., duplicate resource)
            InvalidOperationException when exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => CreateErrorResponse(
                    context,
                    HttpStatusCode.Conflict,
                    "Conflict",
                    exception.Message,
                    correlationId,
                    exception),

            // Timeout exceptions
            TimeoutException or OperationCanceledException => CreateErrorResponse(
                context,
                HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "The request took too long to process",
                correlationId,
                exception),

            // Generic internal server error
            _ => CreateErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing your request",
                correlationId,
                exception)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = errorResponse.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }

    private ErrorResponseDto CreateErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string error,
        string message,
        string? correlationId,
        Exception exception)
    {
        return new ErrorResponseDto
        {
            Error = error,
            Message = message,
            StatusCode = (int)statusCode,
            CorrelationId = correlationId,
            Path = context.Request.Path,
            // Only include stack trace in development
            StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
        };
    }

    private ErrorResponseDto CreateValidationErrorResponse(
        HttpContext context,
        ValidationException validationException,
        string? correlationId)
    {
        var validationErrors = validationException.Errors
            .Select(error => new ValidationErrorDto
            {
                Field = error.PropertyName,
                Message = error.ErrorMessage,
                AttemptedValue = error.AttemptedValue
            })
            .ToList();

        var details = new Dictionary<string, object>
        {
            ["validationErrors"] = validationErrors
        };

        return new ErrorResponseDto
        {
            Error = "Validation Error",
            Message = "One or more validation errors occurred",
            StatusCode = (int)HttpStatusCode.BadRequest,
            CorrelationId = correlationId,
            Path = context.Request.Path,
            Details = details,
            StackTrace = _environment.IsDevelopment() ? validationException.StackTrace : null
        };
    }
}
