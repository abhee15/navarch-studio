import React, { useRef, useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { Sparkles, MessageSquare, X, ChevronsRight, ChevronsLeft } from "lucide-react";
import { useStore } from "../../../stores";
import { ChatTab } from "./tabs/ChatTab";

export const DockedPanel: React.FC = observer(() => {
  const { copilotStore } = useStore();
  const { panelPosition, panelWidth } = copilotStore;
  const [activeTab] = useState<"chat">("chat"); // Simplified - only chat tab for MVP
  const panelRef = useRef<HTMLDivElement>(null);
  const resizerRef = useRef<HTMLDivElement>(null);

  // Resize handler
  useEffect(() => {
    const resizer = resizerRef.current;
    if (!resizer) return;

    let isResizing = false;
    let startX = 0;
    let startWidth = panelWidth;

    const handleMouseDown = (e: MouseEvent) => {
      isResizing = true;
      startX = e.clientX;
      startWidth = panelWidth;
      document.body.style.cursor = "ew-resize";
      document.body.style.userSelect = "none";
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing) return;

      const delta = panelPosition === "right" ? startX - e.clientX : e.clientX - startX;
      const newWidth = Math.max(300, Math.min(800, startWidth + delta));
      copilotStore.setWidth(newWidth);
    };

    const handleMouseUp = () => {
      isResizing = false;
      document.body.style.cursor = "";
      document.body.style.userSelect = "";
    };

    resizer.addEventListener("mousedown", handleMouseDown);
    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);

    return () => {
      resizer.removeEventListener("mousedown", handleMouseDown);
      document.removeEventListener("mousemove", handleMouseMove);
      document.removeEventListener("mouseup", handleMouseUp);
    };
  }, [panelWidth, panelPosition, copilotStore]);

  const isRight = panelPosition === "right";

  return (
    <div
      ref={panelRef}
      className={`fixed top-0 ${isRight ? "right-0" : "left-0"} h-screen bg-gray-50 ${
        isRight ? "border-l" : "border-r"
      } border-gray-200 shadow-xl z-40 flex flex-col`}
      style={{ width: `${panelWidth}px` }}
    >
      {/* Resize Handle */}
      <div
        ref={resizerRef}
        className={`absolute ${isRight ? "left-0" : "right-0"} top-0 h-full w-1 cursor-ew-resize hover:bg-blue-500 transition-colors z-50`}
        title="Drag to resize"
      />

      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 text-white shadow-md">
        <div className="flex items-center gap-2">
          <Sparkles className="w-5 h-5" />
          <h2 className="font-semibold text-lg">NavArch Copilot</h2>
        </div>

        <div className="flex items-center gap-1">
          <button
            onClick={() => copilotStore.setPosition("minimized")}
            className="p-1.5 hover:bg-white/20 rounded transition-colors"
            title="Minimize"
          >
            {isRight ? <ChevronsRight className="w-4 h-4" /> : <ChevronsLeft className="w-4 h-4" />}
          </button>
          <button
            onClick={() => copilotStore.setPosition("hidden")}
            className="p-1.5 hover:bg-white/20 rounded transition-colors"
            title="Close"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Tab Navigation (Simplified - single tab for MVP) */}
      <div className="flex border-b border-gray-200 bg-white">
        <button className="flex-1 flex items-center justify-center gap-2 px-4 py-3 text-sm font-medium text-blue-600 border-b-2 border-blue-600 bg-blue-50">
          <MessageSquare className="w-4 h-4" />
          <span>Chat</span>
        </button>
      </div>

      {/* Tab Content */}
      <div className="flex-1 overflow-hidden">{activeTab === "chat" && <ChatTab />}</div>

      {/* Footer (Status Bar) */}
      <div className="flex items-center justify-between px-4 py-2 bg-gray-100 border-t border-gray-200 text-xs text-gray-600">
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1">
            <span className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
            Online
          </span>
          <span>Model: GPT-4o-mini</span>
        </div>
      </div>
    </div>
  );
});
