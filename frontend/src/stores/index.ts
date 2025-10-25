import { AuthStore } from "./AuthStore";
import { DataStore } from "./DataStore";

export class RootStore {
  authStore: AuthStore;
  dataStore: DataStore;

  constructor() {
    this.authStore = new AuthStore();
    this.dataStore = new DataStore();
  }
}

export const rootStore = new RootStore();
export const useStore = () => rootStore;
