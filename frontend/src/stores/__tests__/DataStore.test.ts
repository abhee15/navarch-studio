import { DataStore } from "../DataStore";

describe("DataStore", () => {
  let dataStore: DataStore;

  beforeEach(() => {
    dataStore = new DataStore();
  });

  it("should initialize with default values", () => {
    expect(dataStore.loading).toBe(false);
    expect(dataStore.error).toBeNull();
  });

  // Add tests as data-related methods are implemented
});
