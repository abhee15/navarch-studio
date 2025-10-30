import "@testing-library/jest-dom";

// Mock config/runtime for tests to avoid import.meta.env access
jest.mock("./config/runtime", () => {
  return {
    getConfig: () => ({ authMode: "local", apiUrl: "http://localhost:5002" }),
    isConfigLoaded: () => false,
  };
});

// Mock Vite's import.meta.env usage via stubbing the API module used by tests
jest.mock("./services/api", () => {
  const noop = () => Promise.resolve({ data: {} });
  return {
    api: {
      get: noop,
      post: noop,
      put: noop,
      delete: noop,
      interceptors: { request: { use: () => {} }, response: { use: () => {} } },
    },
  };
});

// Mock URL functions
global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
// @ts-expect-error: Provided by test environment only
global.URL.revokeObjectURL = jest.fn();

// Avoid navigation on anchor clicks in tests
Object.defineProperty(HTMLAnchorElement.prototype, "click", {
  configurable: true,
  writable: true,
  value: jest.fn(),
});

// Mock exportApi to avoid actual network and return a small blob
jest.mock("./services/hydrostaticsApi", () => {
  const textBlob = (t: string) => new Blob([t], { type: "text/plain" });
  return {
    exportApi: {
      exportCsv: jest.fn(async () => textBlob("csv")),
      exportJson: jest.fn(async () => textBlob("json")),
      exportPdf: jest.fn(async () => textBlob("pdf")),
      exportExcel: jest.fn(async () => textBlob("excel")),
    },
    vesselsApi: {
      // keep as minimal noop for other tests
      list: jest.fn(async () => ({ vessels: [], total: 0 })),
    },
  };
});

// Mock LocalAuthService to avoid network in AuthStore tests
jest.mock("./services/localAuthService", () => {
  let authed = false;
  type TestUser = { id: string; email: string; name: string };
  let user: TestUser | null = null;
  return {
    LocalAuthService: {
      login: jest.fn(async () => {
        authed = true;
        user = { id: "1", email: "test@example.com", name: "Test User" };
        return user;
      }),
      getUser: jest.fn((): TestUser | null => (authed ? user : null)),
      isAuthenticated: jest.fn(() => authed),
      getToken: jest.fn((): string | null => (authed ? "fake-jwt" : null)),
      logout: jest.fn(() => {
        authed = false;
        user = null;
      }),
      setToken: jest.fn(),
      setUser: jest.fn((u: TestUser) => {
        user = u;
      }),
      getCurrentUser: jest.fn(async (): Promise<TestUser> =>
        user || { id: "1", email: "test@example.com", name: "Test User" }
      ),
    },
  };
});
