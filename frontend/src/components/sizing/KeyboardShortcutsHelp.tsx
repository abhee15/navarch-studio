import { useState, useEffect } from "react";

/**
 * Keyboard Shortcuts Help Modal
 *
 * Shows available keyboard shortcuts for power users
 */
export const KeyboardShortcutsHelp: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Toggle help with '?' key
      if (e.key === "?" && !e.ctrlKey && !e.metaKey) {
        const target = e.target as HTMLElement;
        // Don't trigger if user is typing in an input
        if (target.tagName !== "INPUT" && target.tagName !== "TEXTAREA") {
          e.preventDefault();
          setIsOpen((prev) => !prev);
        }
      }

      // Close help with Escape
      if (e.key === "Escape" && isOpen) {
        setIsOpen(false);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen]);

  if (!isOpen) {
    return (
      <button
        onClick={() => setIsOpen(true)}
        className="fixed bottom-4 right-4 z-40 rounded-full bg-blue-600 p-3 text-white shadow-lg hover:bg-blue-700 transition-all hover:scale-110"
        title="Keyboard Shortcuts (Press ?)"
      >
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
      </button>
    );
  }

  const shortcuts = [
    {
      category: "Navigation",
      items: [
        { keys: ["?"], description: "Show/hide keyboard shortcuts" },
        { keys: ["Esc"], description: "Close modal or return to previous view" },
        { keys: ["G", "M"], description: "Go to Mission List" },
        { keys: ["G", "W"], description: "Go to Wizard (New Mission)" },
      ],
    },
    {
      category: "Viewport Controls",
      items: [
        { keys: ["1"], description: "Maximize Plan View" },
        { keys: ["2"], description: "Maximize Profile View" },
        { keys: ["3"], description: "Maximize Sections View" },
        { keys: ["4"], description: "Maximize 3D View" },
        { keys: ["Q"], description: "Return to Quad View" },
        { keys: ["E"], description: "Export current view (SVG)" },
        { keys: ["Shift", "E"], description: "Export current view (PNG)" },
      ],
    },
    {
      category: "Workspace",
      items: [
        { keys: ["C"], description: "Toggle Comparison Mode" },
        { keys: ["T"], description: "Toggle between KPI / Offsets table" },
        { keys: ["R"], description: "Re-run solver with current parameters" },
        { keys: ["Ctrl", "S"], description: "Save changes" },
      ],
    },
    {
      category: "Selection",
      items: [
        { keys: ["←", "→"], description: "Navigate between candidates" },
        { keys: ["Space"], description: "Select/deselect for comparison" },
        { keys: ["Enter"], description: "Open selected candidate workspace" },
      ],
    },
  ];

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm"
        onClick={() => setIsOpen(false)}
      />

      {/* Modal */}
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
        <div className="pointer-events-auto max-w-3xl w-full bg-white dark:bg-gray-800 rounded-xl shadow-2xl overflow-hidden animate-zoomIn">
          {/* Header */}
          <div className="bg-gradient-to-r from-blue-600 to-cyan-600 p-6 text-white">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-2xl font-bold">⌨️ Keyboard Shortcuts</h2>
                <p className="text-sm opacity-90 mt-1">Power user commands for faster workflow</p>
              </div>
              <button
                onClick={() => setIsOpen(false)}
                className="rounded-lg p-2 hover:bg-white/20 transition-colors"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>
          </div>

          {/* Content */}
          <div className="p-6 max-h-[600px] overflow-y-auto">
            <div className="space-y-6">
              {shortcuts.map((category) => (
                <div key={category.category}>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                    <span className="w-1 h-6 bg-blue-600 rounded-full"></span>
                    {category.category}
                  </h3>
                  <div className="space-y-2">
                    {category.items.map((item, idx) => (
                      <div
                        key={idx}
                        className="flex items-center justify-between py-2 px-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                      >
                        <span className="text-sm text-gray-700 dark:text-gray-300">
                          {item.description}
                        </span>
                        <div className="flex gap-1">
                          {item.keys.map((key, keyIdx) => (
                            <span
                              key={keyIdx}
                              className="inline-flex items-center justify-center min-w-[32px] px-2 py-1 text-xs font-bold text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded shadow-sm"
                            >
                              {key}
                            </span>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Footer */}
          <div className="bg-gray-50 dark:bg-gray-900 p-4 border-t border-gray-200 dark:border-gray-700">
            <p className="text-xs text-gray-600 dark:text-gray-400 text-center">
              💡 Tip: Press{" "}
              <kbd className="px-2 py-1 text-xs bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded">
                ?
              </kbd>{" "}
              anytime to toggle this help
            </p>
          </div>
        </div>
      </div>
    </>
  );
};
