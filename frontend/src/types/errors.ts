/**
 * Standardized error response from API endpoints
 * Matches the backend ErrorResponseDto structure
 */
export interface ApiErrorResponse {
  error: string;
  message: string;
  statusCode: number;
  correlationId?: string;
  timestamp: string;
  path?: string;
  details?: Record<string, unknown>;
  stackTrace?: string;
}

/**
 * Validation error details for field-specific errors
 */
export interface ValidationError {
  field: string;
  message: string;
  attemptedValue?: unknown;
}

/**
 * Custom error class for API errors
 * Provides type-safe access to error response data
 */
export class ApiError extends Error {
  public readonly statusCode: number;
  public readonly error: string;
  public readonly correlationId?: string;
  public readonly timestamp: string;
  public readonly path?: string;
  public readonly details?: Record<string, unknown>;
  public readonly validationErrors?: ValidationError[];

  constructor(response: ApiErrorResponse) {
    super(response.message);
    this.name = "ApiError";
    this.statusCode = response.statusCode;
    this.error = response.error;
    this.correlationId = response.correlationId;
    this.timestamp = response.timestamp;
    this.path = response.path;
    this.details = response.details;

    // Extract validation errors if present
    if (response.details?.validationErrors) {
      this.validationErrors = response.details.validationErrors as ValidationError[];
    }

    // Maintain proper stack trace for where our error was thrown
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, ApiError);
    }
  }

  /**
   * Check if this is a validation error
   */
  isValidationError(): boolean {
    return this.statusCode === 400 && this.error === "Validation Error";
  }

  /**
   * Check if this is an authentication error
   */
  isAuthError(): boolean {
    return this.statusCode === 401;
  }

  /**
   * Check if this is a not found error
   */
  isNotFoundError(): boolean {
    return this.statusCode === 404;
  }

  /**
   * Check if this is a server error
   */
  isServerError(): boolean {
    return this.statusCode >= 500;
  }

  /**
   * Get a formatted error message suitable for display to users
   */
  getUserMessage(): string {
    if (this.isValidationError() && this.validationErrors) {
      const fieldErrors = this.validationErrors
        .map((err) => `${err.field}: ${err.message}`)
        .join(", ");
      return `Validation failed: ${fieldErrors}`;
    }

    return this.message;
  }

  /**
   * Get validation error for a specific field
   */
  getFieldError(fieldName: string): string | undefined {
    if (!this.validationErrors) return undefined;

    const error = this.validationErrors.find(
      (err) => err.field.toLowerCase() === fieldName.toLowerCase()
    );
    return error?.message;
  }
}

/**
 * Error categories for better error handling
 */
export enum ErrorCategory {
  NETWORK = "NETWORK",
  VALIDATION = "VALIDATION",
  AUTHENTICATION = "AUTHENTICATION",
  AUTHORIZATION = "AUTHORIZATION",
  NOT_FOUND = "NOT_FOUND",
  SERVER = "SERVER",
  UNKNOWN = "UNKNOWN",
}

/**
 * Categorize an error for consistent handling
 */
export function categorizeError(error: unknown): ErrorCategory {
  if (error instanceof ApiError) {
    if (error.isValidationError()) return ErrorCategory.VALIDATION;
    if (error.isAuthError()) return ErrorCategory.AUTHENTICATION;
    if (error.statusCode === 403) return ErrorCategory.AUTHORIZATION;
    if (error.isNotFoundError()) return ErrorCategory.NOT_FOUND;
    if (error.isServerError()) return ErrorCategory.SERVER;
  }

  // Check for network errors (Axios)
  if (error && typeof error === "object" && "code" in error) {
    const axiosError = error as { code?: string };
    if (
      axiosError.code === "ERR_NETWORK" ||
      axiosError.code === "ECONNABORTED" ||
      axiosError.code === "ETIMEDOUT"
    ) {
      return ErrorCategory.NETWORK;
    }
  }

  return ErrorCategory.UNKNOWN;
}

/**
 * Get a user-friendly error message based on error category
 */
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.getUserMessage();
  }

  const category = categorizeError(error);

  switch (category) {
    case ErrorCategory.NETWORK:
      return "Unable to connect to the server. Please check your internet connection.";
    case ErrorCategory.AUTHENTICATION:
      return "Your session has expired. Please log in again.";
    case ErrorCategory.AUTHORIZATION:
      return "You don't have permission to perform this action.";
    case ErrorCategory.NOT_FOUND:
      return "The requested resource was not found.";
    case ErrorCategory.SERVER:
      return "A server error occurred. Please try again later.";
    case ErrorCategory.VALIDATION:
      return "Please check your input and try again.";
    default:
      if (error instanceof Error) {
        return error.message;
      }
      return "An unexpected error occurred. Please try again.";
  }
}

/**
 * Extract correlation ID from error for debugging
 */
export function getCorrelationId(error: unknown): string | undefined {
  if (error instanceof ApiError) {
    return error.correlationId;
  }
  return undefined;
}
