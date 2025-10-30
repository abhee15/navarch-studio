import React, { Component, ErrorInfo, ReactNode } from "react";
import { getErrorMessage, getCorrelationId } from "../types/errors";

interface Props {
  children: ReactNode;
  fallback?: (error: Error, errorInfo: ErrorInfo, reset: () => void) => ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

/**
 * Error Boundary component that catches React rendering errors
 * and displays a user-friendly error message instead of crashing the app
 */
export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI
    return { hasError: true, error, errorInfo: null };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log the error to console (in production, you might want to send this to an error tracking service)
    console.error("Error Boundary caught an error:", error, errorInfo);

    this.setState({
      hasError: true,
      error,
      errorInfo,
    });
  }

  private handleReset = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  public render() {
    if (this.state.hasError && this.state.error) {
      // If a custom fallback is provided, use it
      if (this.props.fallback) {
        return this.props.fallback(this.state.error, this.state.errorInfo!, this.handleReset);
      }

      // Default error UI
      const correlationId = getCorrelationId(this.state.error);
      const errorMessage = getErrorMessage(this.state.error);

      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4">
          <div className="max-w-md w-full space-y-6">
            <div className="text-center">
              <div className="mx-auto h-12 w-12 text-red-600 dark:text-red-400">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
              </div>
              <h2 className="mt-6 text-3xl font-extrabold text-gray-900 dark:text-white">
                Something went wrong
              </h2>
              <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">{errorMessage}</p>
              {correlationId && (
                <p className="mt-2 text-xs text-gray-500 dark:text-gray-500">
                  Error ID: {correlationId}
                </p>
              )}
            </div>

            <div className="space-y-3">
              <button
                onClick={this.handleReset}
                className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                Try again
              </button>
              <button
                onClick={() => (window.location.href = "/")}
                className="w-full flex justify-center py-2 px-4 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                Go to home
              </button>
            </div>

            {process.env.NODE_ENV === "development" && this.state.errorInfo && (
              <details className="mt-6 text-xs text-gray-600 dark:text-gray-400">
                <summary className="cursor-pointer font-semibold">
                  Error details (development only)
                </summary>
                <pre className="mt-2 whitespace-pre-wrap overflow-auto bg-gray-100 dark:bg-gray-800 p-4 rounded">
                  {this.state.error.toString()}
                  {"\n\n"}
                  {this.state.errorInfo.componentStack}
                </pre>
              </details>
            )}
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

/**
 * Hook-based component for easier usage within functional components
 */
export function withErrorBoundary<P extends object>(
  Component: React.ComponentType<P>,
  fallback?: (error: Error, errorInfo: ErrorInfo, reset: () => void) => ReactNode
) {
  return function WithErrorBoundary(props: P) {
    return (
      <ErrorBoundary fallback={fallback}>
        <Component {...props} />
      </ErrorBoundary>
    );
  };
}
