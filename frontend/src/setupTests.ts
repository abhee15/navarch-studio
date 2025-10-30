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

// Mock URL.createObjectURL to return a stable value
global.URL.createObjectURL = jest.fn(() => "blob:mock-url");
// Mock URL.revokeObjectURL to no-op
global.URL.revokeObjectURL = jest.fn();

// Avoid navigation on anchor clicks in tests
Object.defineProperty(HTMLAnchorElement.prototype, "click", {
  configurable: true,
  writable: true,
  value: jest.fn(),
});

// Mock exportApi to avoid actual network and return a small blob
const exportApiMock = {
  exportCsv: jest.fn(async () => new Blob(["csv"], { type: "text/plain" })),
  exportJson: jest.fn(async () => new Blob(["json"], { type: "application/json" })),
  exportPdf: jest.fn(async () => new Blob(["pdf"], { type: "application/pdf" })),
  exportExcel: jest.fn(async () => new Blob(["excel"], { type: "application/vnd.ms-excel" })),
};

// Cover different relative import paths used across components
jest.mock("./services/hydrostaticsApi", () => ({ exportApi: exportApiMock, vesselsApi: { list: jest.fn(async () => ({ vessels: [], total: 0 })) } }));
jest.mock("../services/hydrostaticsApi", () => ({ exportApi: exportApiMock, vesselsApi: { list: jest.fn(async () => ({ vessels: [], total: 0 })) } }));
jest.mock("../../services/hydrostaticsApi", () => ({ exportApi: exportApiMock, vesselsApi: { list: jest.fn(async () => ({ vessels: [], total: 0 })) } }));

// Mock LocalAuthService to avoid network in AuthStore tests
jest.mock("./services/localAuthService", () => {
  const user = { id: "1", email: "test@example.com", name: "Test User" };
  return {
    LocalAuthService: {
      login: jest.fn(async () => user),
      getUser: jest.fn(() => user),
      isAuthenticated: jest.fn(() => true),
      getToken: jest.fn(() => "fake-jwt"),
      logout: jest.fn(),
      setToken: jest.fn(),
      setUser: jest.fn(),
      getCurrentUser: jest.fn(async () => user),
    },
  };
});
