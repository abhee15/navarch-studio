import { makeAutoObservable } from "mobx";

/**
 * DataStore for managing general application data.
 * This store can be extended to handle shared data across the application.
 */
export class DataStore {
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  // Add data-related methods as needed for your application
}
