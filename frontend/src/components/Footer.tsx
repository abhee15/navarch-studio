import React from "react";

/**
 * Footer component with dynamic copyright year
 */
export const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="border-t border-gray-200 dark:border-gray-700 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm mt-auto">
      <div className="container mx-auto px-4 py-6">
        <p className="text-center text-sm text-muted-foreground dark:text-gray-400">
          © {currentYear} NavArch Studio. All rights reserved.
        </p>
      </div>
    </footer>
  );
};
