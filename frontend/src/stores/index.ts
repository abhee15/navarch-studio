import { AuthStore } from "./AuthStore";
import { DataStore } from "./DataStore";
import { SizingStore } from "./SizingStore";
import { CopilotStore } from "./CopilotStore";

export class RootStore {
  authStore: AuthStore;
  dataStore: DataStore;
  sizingStore: SizingStore;
  copilotStore: CopilotStore;

  constructor() {
    this.authStore = new AuthStore();
    this.dataStore = new DataStore();
    this.sizingStore = new SizingStore();
    this.copilotStore = new CopilotStore();
  }
}

export const rootStore = new RootStore();
export const useStore = () => rootStore;
