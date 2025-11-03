import { AuthStore } from "./AuthStore";
import { DataStore } from "./DataStore";
import { SizingStore } from "./SizingStore";

export class RootStore {
  authStore: AuthStore;
  dataStore: DataStore;
  sizingStore: SizingStore;

  constructor() {
    this.authStore = new AuthStore();
    this.dataStore = new DataStore();
    this.sizingStore = new SizingStore();
  }
}

export const rootStore = new RootStore();
export const useStore = () => rootStore;
