import { AuthStore } from "../AuthStore";

describe("AuthStore", () => {
  let authStore: AuthStore;

  beforeEach(() => {
    authStore = new AuthStore();
  });

  it("should initialize with default values", () => {
    expect(authStore.user).toBeNull();
    expect(authStore.isAuthenticated).toBe(false);
    expect(authStore.loading).toBe(false);
    expect(authStore.error).toBeNull();
  });

  it("should login successfully", async () => {
    await authStore.login("test@example.com", "password");

    expect(authStore.isAuthenticated).toBe(true);
    expect(authStore.user).toBeDefined();
    expect(authStore.user?.email).toBe("test@example.com");
  });

  it("should logout", () => {
    authStore.user = { id: "1", email: "test@example.com", name: "Test" };
    authStore.isAuthenticated = true;

    authStore.logout();

    expect(authStore.user).toBeNull();
    expect(authStore.isAuthenticated).toBe(false);
  });
});





