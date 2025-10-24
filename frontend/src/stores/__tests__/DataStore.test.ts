import { DataStore } from "../DataStore";

describe("DataStore", () => {
  let dataStore: DataStore;

  beforeEach(() => {
    dataStore = new DataStore();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it("should initialize with default values", () => {
    expect(dataStore.products).toEqual([]);
    expect(dataStore.loading).toBe(false);
    expect(dataStore.error).toBeNull();
  });

  it("should fetch products successfully", async () => {
    // Start fetch
    const fetchPromise = dataStore.fetchProducts();

    // Should be loading
    expect(dataStore.loading).toBe(true);
    expect(dataStore.error).toBeNull();

    // Fast-forward time
    jest.runAllTimers();
    await fetchPromise;

    // Should have products
    expect(dataStore.loading).toBe(false);
    expect(dataStore.products).toHaveLength(2);
    expect(dataStore.products[0]).toMatchObject({
      id: "1",
      name: "Product 1",
      price: 29.99,
      description: "Description 1",
    });
    expect(dataStore.products[1]).toMatchObject({
      id: "2",
      name: "Product 2",
      price: 39.99,
      description: "Description 2",
    });
  });

  it("should handle loading state correctly", () => {
    // Start fetch
    dataStore.fetchProducts();

    // Should be loading immediately
    expect(dataStore.loading).toBe(true);
    expect(dataStore.error).toBeNull();
  });

  it("should clear previous products when fetching", async () => {
    // Set initial products
    dataStore.products = [{ id: "old", name: "Old Product", price: 10, description: "Old" }];

    // Start fetch
    const fetchPromise = dataStore.fetchProducts();
    jest.runAllTimers();
    await fetchPromise;

    // Should have new products
    expect(dataStore.products).toHaveLength(2);
    expect(dataStore.products.find((p) => p.id === "old")).toBeUndefined();
  });
});





