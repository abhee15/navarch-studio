import { makeObservable, observable, action, runInAction } from "mobx";
import { ChatMessage, PanelPosition, MissionSuggestionResponse } from "../types/ai";
import { MissionCase } from "../types/sizing";
import { aiAgentApi } from "../services/aiAgentApi";
import toast from "react-hot-toast";

export class CopilotStore {
  @observable panelPosition: PanelPosition = "right";
  @observable panelWidth: number = 400;
  @observable floatingPosition: { x: number; y: number } = { x: 100, y: 100 };
  @observable messages: ChatMessage[] = [];
  @observable isLoading: boolean = false;
  @observable generatedMission: MissionCase | null = null;
  @observable currentConversationId: string | null = null;

  constructor() {
    makeObservable(this);
    this.loadFromLocalStorage();
    this.initializeWelcomeMessage();
  }

  @action
  initializeWelcomeMessage() {
    if (this.messages.length === 0) {
      this.messages.push({
        id: "1",
        role: "assistant",
        content: `👋 Hi! I'm your NavArch Copilot. I can help you:

- **Design vessels** from natural language descriptions
- **Generate mission parameters** for hull sizing
- **Suggest improvements** to your designs

What would you like to work on today?

Try: "Design a 500 TEU container ship for coastal routes"`,
        timestamp: new Date(),
      });
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
    const savedPosition = localStorage.getItem("copilot-panel-position") as PanelPosition;
    if (savedPosition) {
      this.panelPosition = savedPosition;
    }

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
