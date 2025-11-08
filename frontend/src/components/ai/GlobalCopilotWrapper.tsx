import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useLocation } from "react-router-dom";
import { useStore } from "../../stores";
import { CopilotPanel } from "./CopilotPanel/CopilotPanel";

export const GlobalCopilotWrapper: React.FC<{ children: React.ReactNode }> = observer(
  ({ children }) => {
    const location = useLocation();
    const { copilotStore } = useStore();
    const [isMobile, setIsMobile] = useState(window.innerWidth < 768);

    // Mobile detection - hide AI below 768px
    useEffect(() => {
      const handleResize = () => {
        setIsMobile(window.innerWidth < 768);
      };

      window.addEventListener("resize", handleResize);
      return () => window.removeEventListener("resize", handleResize);
    }, []);

    // Determine if we should show AI on this page
    const shouldShowAI = () => {
      // Hide on mobile devices
      if (isMobile) {
        return false;
      }

      const path = location.pathname;

      // EXCLUDE: Login, signup, dashboard, and listing pages
      const excludedPaths = [
        "/login",
        "/signup",
        "/dashboard",
        "/sizing/wizard", // Wizard page - users build briefs from missions page
        "/hydrostatics/vessels", // Vessels list
        "/resistance/vessels", // Vessels list
        "/catalog/ml-hulls", // Just listing
        "/benchmarks", // Benchmarks list
      ];

      // Exact match for excluded paths
      if (excludedPaths.includes(path)) {
        return false;
      }

      // INCLUDE: Actual workspace/detail pages only
      const workspacePatterns = [
        /\/sizing\/missions/, // Mission list page - users build briefs here
        /\/sizing\/mission\/new/,
        /\/sizing\/runs\/[^/]+/,
        /\/sizing\/workspace\/[^/]+/,
        /\/sizing\/explorer\/[^/]+/,
        /\/hydrostatics\/vessels\/[^/]+/,
        /\/resistance\/vessels\/[^/]+/,
        /\/catalog\/hulls\/[^/]+/, // Hull detail page
        /\/catalog\/browse/, // Browsing with filters
        /\/benchmarks\/[^/]+/, // Benchmark detail
      ];

      return workspacePatterns.some((pattern) => pattern.test(path));
    };

    // Update context based on current route
    useEffect(() => {
      const path = location.pathname;

      if (path.includes("/sizing") || path.includes("/hull-sizing")) {
        copilotStore.setContext("hull-sizing");
      } else if (path.includes("/hydrostatics")) {
        copilotStore.setContext("hydrostatics");
      } else if (path.includes("/resistance")) {
        copilotStore.setContext("resistance");
      } else if (path.includes("/catalog") || path.includes("/benchmarks")) {
        copilotStore.setContext("catalog");
      } else {
        copilotStore.setContext("general");
      }
    }, [location.pathname, copilotStore]);

    // Keyboard shortcuts
    useEffect(() => {
      const handleKeyDown = (e: KeyboardEvent) => {
        // Cmd+K (Mac) or Ctrl+K (Windows/Linux) to toggle panel
        if ((e.metaKey || e.ctrlKey) && e.key === "k") {
          e.preventDefault();
          if (copilotStore.panelPosition === "hidden") {
            copilotStore.setPosition("right");
          } else {
            copilotStore.setPosition("hidden");
          }
        }

        // Escape to close panel (if it's open)
        if (e.key === "Escape" && copilotStore.panelPosition !== "hidden") {
          copilotStore.setPosition("hidden");
        }
      };

      document.addEventListener("keydown", handleKeyDown);
      return () => document.removeEventListener("keydown", handleKeyDown);
    }, [copilotStore]);

    const showAI = shouldShowAI();
    const isPanelOpen = showAI && copilotStore.panelPosition !== "hidden";

    return (
      <div className="flex h-screen overflow-hidden">
        {/* Main content area */}
        <div
          className="flex-1 overflow-auto transition-all duration-300 ease-in-out"
          style={{
            marginRight: isPanelOpen ? `${copilotStore.panelWidth}px` : "0px",
          }}
        >
          {children}
        </div>

        {/* AI Panel */}
        {showAI && <CopilotPanel />}
      </div>
    );
  }
);
