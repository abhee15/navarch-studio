namespace Shared.DTOs;

/// <summary>
/// Standardized error response for all API endpoints.
/// Provides consistent error structure across the application.
/// </summary>
public record ErrorResponseDto
{
    /// <summary>
    /// Brief error title/type (e.g., "Validation Error", "Not Found", "Internal Server Error")
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Detailed human-readable error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// HTTP status code (e.g., 400, 404, 500)
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Correlation ID for tracing requests across services
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Timestamp when the error occurred (ISO 8601 format)
    /// </summary>
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");

    /// <summary>
    /// Request path where the error occurred
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Additional error details (validation errors, field-specific errors, etc.)
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }

    /// <summary>
    /// Stack trace (only included in development environment)
    /// </summary>
    public string? StackTrace { get; init; }
}

/// <summary>
/// Validation error details for field-specific errors
/// </summary>
public record ValidationErrorDto
{
    /// <summary>
    /// Field name that failed validation
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Validation error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Attempted value that failed validation
    /// </summary>
    public object? AttemptedValue { get; init; }
}
