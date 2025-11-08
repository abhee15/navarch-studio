import React, { useState, useRef, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { Send, RotateCcw } from "lucide-react";
import ReactMarkdown from "react-markdown";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { vscDarkPlus } from "react-syntax-highlighter/dist/esm/styles/prism";
import { useStore } from "../../../../stores";
import { ChatMessage as ChatMessageType } from "../../../../types/ai";

const ChatMessage: React.FC<{ message: ChatMessageType }> = ({ message }) => {
  const isUser = message.role === "user";

  return (
    <div className={`flex ${isUser ? "justify-end" : "justify-start"}`}>
      <div
        className={`max-w-[85%] rounded-lg p-2.5 ${
          isUser ? "bg-primary text-primary-foreground" : "bg-card border border-border"
        }`}
      >
        {isUser ? (
          <p className="text-sm leading-snug whitespace-pre-wrap">{message.content}</p>
        ) : (
          <div className="prose prose-sm max-w-none dark:prose-invert prose-p:my-1 prose-headings:my-2">
            <ReactMarkdown
              components={{
                code(props) {
                  const { children, className } = props;
                  const match = /language-(\w+)/.exec(className || "");
                  return match ? (
                    <SyntaxHighlighter style={vscDarkPlus} language={match[1]} PreTag="div">
                      {String(children).replace(/\n$/, "")}
                    </SyntaxHighlighter>
                  ) : (
                    <code className={className}>{children}</code>
                  );
                },
              }}
            >
              {message.content}
            </ReactMarkdown>
          </div>
        )}

        <p
          className={`text-xs mt-1.5 pt-1.5 border-t ${isUser ? "border-primary-foreground/30 text-primary-foreground/70" : "border-border text-muted-foreground"}`}
        >
          {message.timestamp.toLocaleTimeString()}
        </p>
      </div>
    </div>
  );
};

export const ChatTab: React.FC = observer(() => {
  const { copilotStore } = useStore();
  const [input, setInput] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [copilotStore.messages]);

  // Auto-resize textarea
  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.style.height = "auto";
      inputRef.current.style.height = `${inputRef.current.scrollHeight}px`;
    }
  }, [input]);

  const handleSend = async () => {
    if (!input.trim() || copilotStore.isLoading) return;

    await copilotStore.sendMessage(input);
    setInput("");
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // Context-aware quick suggestions (reduced to 2 per context)
  const getQuickSuggestions = () => {
    switch (copilotStore.currentContext) {
      case "hull-sizing":
        return ["Design a 500 TEU container ship", "Bulk carrier for 80,000 tonnes"];
      case "hydrostatics":
        return ["Explain GMt and why it matters", "How to improve stability"];
      case "resistance":
        return ["Optimize my vessel's speed", "How to reduce fuel consumption"];
      case "catalog":
        return ["Find similar hulls", "Typical Cb for container ships"];
      default:
        return ["Design a 500 TEU container ship", "Explain stability calculations"];
    }
  };

  const quickSuggestions = getQuickSuggestions();

  return (
    <div className="flex flex-col h-full bg-background">
      {/* Messages Area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {copilotStore.messages.map((msg) => (
          <ChatMessage key={msg.id} message={msg} />
        ))}

        {copilotStore.isLoading && (
          <div className="flex items-center gap-2 text-muted-foreground">
            <div className="flex gap-1">
              <span
                className="w-2 h-2 bg-primary rounded-full animate-bounce"
                style={{ animationDelay: "0ms" }}
              />
              <span
                className="w-2 h-2 bg-primary rounded-full animate-bounce"
                style={{ animationDelay: "150ms" }}
              />
              <span
                className="w-2 h-2 bg-primary rounded-full animate-bounce"
                style={{ animationDelay: "300ms" }}
              />
            </div>
            <span className="text-sm">AI is thinking...</span>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input Area */}
      <div className="border-t border-border bg-card p-4">
        {/* Input Field */}
        <div className="flex gap-2">
          <textarea
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask me anything... (Shift+Enter for new line)"
            className="flex-1 px-3 py-2 border border-input rounded-md bg-background text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-input resize-none max-h-32"
            rows={1}
            disabled={copilotStore.isLoading}
          />
          <button
            onClick={handleSend}
            disabled={copilotStore.isLoading || !input.trim()}
            className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors self-end text-sm font-medium"
          >
            <Send className="w-4 h-4" />
          </button>
        </div>

        {/* Quick Actions */}
        <div className="flex items-center justify-between mt-2">
          <div className="flex gap-1.5 flex-wrap">
            {quickSuggestions.map((suggestion, idx) => (
              <button
                key={idx}
                onClick={() => setInput(suggestion)}
                className="text-xs px-1.5 py-0.5 bg-accent hover:bg-accent/80 rounded text-accent-foreground transition-colors"
              >
                {suggestion}
              </button>
            ))}
          </div>

          <button
            onClick={() => copilotStore.clearChat()}
            className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1 transition-colors flex-shrink-0 ml-2"
            title="Clear chat"
          >
            <RotateCcw className="w-3 h-3" />
            <span className="hidden sm:inline">Clear</span>
          </button>
        </div>
      </div>
    </div>
  );
});
