import React, { useEffect } from "react";
import { observer } from "mobx-react-lite";
import { useLocation } from "react-router-dom";
import { useStore } from "../../stores";
import { CopilotPanel } from "./CopilotPanel/CopilotPanel";

export const GlobalCopilotWrapper: React.FC<{ children: React.ReactNode }> = observer(
  ({ children }) => {
    const location = useLocation();
    const { copilotStore } = useStore();

    // Determine if we should show AI on this page
    const shouldShowAI = () => {
      const path = location.pathname;

      // EXCLUDE: Login, signup, dashboard, and listing pages
      const excludedPaths = [
        "/login",
        "/signup",
        "/dashboard",
        "/sizing/missions", // Mission list page
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
        /\/sizing\/wizard/,
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

    return (
      <>
        {children}
        {shouldShowAI() && <CopilotPanel />}
      </>
    );
  }
);
