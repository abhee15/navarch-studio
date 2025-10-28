import { Toaster } from "react-hot-toast";

/**
 * Toast notification provider component
 * Wrap your app with this component to enable toast notifications
 */
export function ToastProvider() {
  return (
    <Toaster
      position="top-right"
      toastOptions={{
        // Default options for all toasts
        duration: 4000,
        style: {
          background: "#363636",
          color: "#fff",
        },
        // Success toasts
        success: {
          duration: 3000,
          iconTheme: {
            primary: "#10b981",
            secondary: "#fff",
          },
        },
        // Error toasts
        error: {
          duration: 5000,
          iconTheme: {
            primary: "#ef4444",
            secondary: "#fff",
          },
        },
        // Loading toasts
        loading: {
          iconTheme: {
            primary: "#3b82f6",
            secondary: "#fff",
          },
        },
      }}
    />
  );
}

// Export toast helper for convenience
export { toast } from "react-hot-toast";
