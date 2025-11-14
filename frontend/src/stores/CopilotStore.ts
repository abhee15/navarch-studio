import { makeObservable, observable, action, runInAction } from "mobx";
import type { ChatMessage, PanelPosition, MissionSuggestionResponse } from "../types/ai";
import type { MissionCase } from "../types/sizing";
import { aiAgentApi } from "../services/aiAgentApi";
import toast from "react-hot-toast";

export class CopilotStore {
  @observable panelPosition: PanelPosition = "hidden";
  @observable panelWidth: number = 400;
  @observable floatingPosition: { x: number; y: number } = { x: 100, y: 100 };
  @observable messages: ChatMessage[] = [];
  @observable isLoading: boolean = false;
  @observable generatedMission: MissionCase | null = null;
  @observable currentConversationId: string | null = null;
  @observable currentContext: string = "general"; // hull-sizing, hydrostatics, resistance, catalog

  constructor() {
    makeObservable(this);
    this.loadFromLocalStorage();
    this.initializeWelcomeMessage();
  }

  @action
  setContext(context: string) {
    if (this.currentContext !== context) {
      this.currentContext = context;
      this.updateWelcomeMessageForContext();
    }
  }

  @action
  initializeWelcomeMessage() {
    if (this.messages.length === 0) {
      this.addWelcomeMessage();
    }
  }

  @action
  updateWelcomeMessageForContext() {
    // Replace first message if it's the welcome message
    if (this.messages.length > 0 && this.messages[0].role === "assistant") {
      this.messages[0] = {
        ...this.messages[0],
        content: this.getWelcomeMessageForContext(),
        timestamp: new Date(),
      };
    }
  }

  private addWelcomeMessage() {
    this.messages.push({
      id: "1",
      role: "assistant",
      content: this.getWelcomeMessageForContext(),
      timestamp: new Date(),
    });
  }

  private getWelcomeMessageForContext(): string {
    switch (this.currentContext) {
      case "hull-sizing":
        return `👋 Hi! I'm your NavArch Copilot for **Hull Sizing**.

I can help you:
- **Design vessels** from natural language descriptions
- **Generate mission parameters** automatically
- **Troubleshoot** solver failures (coming soon)

Try: "Design a 500 TEU container ship for coastal routes"`;

      case "hydrostatics":
        return `👋 Hi! I'm your NavArch Copilot for **Hydrostatics**.

I can help you:
- **Analyze stability** parameters
- **Explain** hydrostatic calculations
- **Suggest** improvements to your vessel design

Try: "Analyze the stability of my current vessel" or "What is GMt?"`;

      case "resistance":
        return `👋 Hi! I'm your NavArch Copilot for **Resistance & Powering**.

I can help you:
- **Optimize** speed and power requirements
- **Explain** resistance calculations
- **Suggest** efficiency improvements

Try: "How can I reduce resistance?" or "What's the optimal speed for my vessel?"`;

      case "catalog":
        return `👋 Hi! I'm your NavArch Copilot for **Catalog**.

I can help you:
- **Find similar hulls** to your requirements
- **Compare** hull characteristics
- **Explain** catalog data

Try: "Find container ship hulls similar to mine" or "What's a good Cb for bulk carriers?"`;

      default:
        return `👋 Hi! I'm your NavArch Copilot.

I can help you across all modules:
- **Hull Sizing:** Design vessels from descriptions
- **Hydrostatics:** Analyze stability and trim
- **Resistance:** Optimize performance
- **Catalog:** Find and compare hulls

What would you like to work on?`;
    }
  }

  @action
  setPosition(position: PanelPosition) {
    this.panelPosition = position;
    localStorage.setItem("copilot-panel-position", position);
  }

  @action
  setWidth(width: number) {
    this.panelWidth = width;
    localStorage.setItem("copilot-panel-width", width.toString());
  }

  @action
  setFloatingPosition(position: { x: number; y: number }) {
    this.floatingPosition = position;
    localStorage.setItem("copilot-floating-position", JSON.stringify(position));
  }

  @action
  async sendMessage(messageContent: string): Promise<void> {
    if (!messageContent.trim()) return;

    // Add user message
    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: "user",
      content: messageContent,
      timestamp: new Date(),
    };

    runInAction(() => {
      this.messages.push(userMessage);
      this.isLoading = true;
    });

    try {
      // Call AI agent API
      const response = await aiAgentApi.suggestMission(messageContent);

      // Format assistant response
      const assistantContent = this.formatMissionResponse(response);

      const assistantMessage: ChatMessage = {
        id: Date.now().toString(),
        role: "assistant",
        content: assistantContent,
        timestamp: new Date(),
      };

      runInAction(() => {
        this.messages.push(assistantMessage);

        // Store generated mission if confidence is good
        if (response.confidence > 0.6) {
          this.generatedMission = response.missionCase;
        }
      });
    } catch (error) {
      console.error("Error sending message to AI:", error);

      const errorMessage: ChatMessage = {
        id: Date.now().toString(),
        role: "assistant",
        content: "❌ Sorry, I encountered an error processing your request. Please try again.",
        timestamp: new Date(),
      };

      runInAction(() => {
        this.messages.push(errorMessage);
      });

      toast.error("Failed to get AI response");
    } finally {
      runInAction(() => {
        this.isLoading = false;
      });
    }
  }

  @action
  clearChat() {
    this.messages = [];
    this.generatedMission = null;
    this.currentConversationId = null;
    this.initializeWelcomeMessage();
  }

  private formatMissionResponse(response: MissionSuggestionResponse): string {
    const m = response.missionCase;
    const confidencePct = (response.confidence * 100).toFixed(0);

    return `I've analyzed your requirements (confidence: ${confidencePct}%):

## Suggested Mission Parameters

**Mission Name**: ${m.name}
**Type**: ${m.missionType}
**Cargo**: ${m.cargoValue.toLocaleString()} ${m.cargoBasis}
**Service Speed**: ${m.serviceSpeedKn} knots
${m.capBeamM ? `**Max Beam**: ${m.capBeamM}m\n` : ""}${m.capDraftM ? `**Max Draft**: ${m.capDraftM}m\n` : ""}

### Reasoning
${response.reasoning}

${
  response.confidence > 0.7
    ? "Would you like me to create this mission and run the hull sizing solver?"
    : "⚠️ Low confidence - please provide more details (cargo type, speed, route, constraints)"
}
`;
  }

  private loadFromLocalStorage() {
    // Don't restore panel position - always start hidden
    // Users must manually toggle visibility each session

    const savedWidth = localStorage.getItem("copilot-panel-width");
    if (savedWidth) {
      this.panelWidth = parseInt(savedWidth, 10);
    }

    const savedFloatingPos = localStorage.getItem("copilot-floating-position");
    if (savedFloatingPos) {
      this.floatingPosition = JSON.parse(savedFloatingPos);
    }
  }
}

export const copilotStore = new CopilotStore();
