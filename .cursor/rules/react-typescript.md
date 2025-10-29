# React + TypeScript Rules

## General Guidelines

- Use functional components with hooks (no class components)
- Use TypeScript strict mode
- Prefer named exports over default exports
- Use explicit return types for functions
- Keep components small and focused (< 200 lines)

## File Naming

- Components: PascalCase (e.g., `UserProfile.tsx`)
- Hooks: camelCase with "use" prefix (e.g., `useAuth.ts`)
- Utils: camelCase (e.g., `formatDate.ts`)
- Types: PascalCase in `types/` folder (e.g., `User.ts`)
- Tests: Same name as file with `.test.tsx` suffix

## Component Structure

```typescript
import React from "react";
import { observer } from "mobx-react-lite";

interface ComponentProps {
  // Props with explicit types
  title: string;
  onClick?: () => void;
}

export const Component: React.FC<ComponentProps> = observer(
  ({ title, onClick }) => {
    // 1. Hooks
    const [state, setState] = React.useState<string>("");

    // 2. Computed values
    const formattedTitle = title.toUpperCase();

    // 3. Handlers
    const handleClick = () => {
      onClick?.();
    };

    // 4. Effects
    React.useEffect(() => {
      // Side effects
    }, []);

    // 5. Render
    return <div onClick={handleClick}>{formattedTitle}</div>;
  }
);

Component.displayName = "Component";
```

## MobX Store Pattern

```typescript
import { makeAutoObservable, runInAction } from "mobx";

export class UserStore {
  users: User[] = [];
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  // Computed
  get activeUsers(): User[] {
    return this.users.filter((u) => u.isActive);
  }

  // Actions
  async fetchUsers(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const response = await api.getUsers();
      runInAction(() => {
        this.users = response.data;
        this.loading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error.message;
        this.loading = false;
      });
    }
  }

  clearError(): void {
    this.error = null;
  }
}
```

## Axios Configuration

```typescript
// services/api.ts
import axios, { AxiosInstance } from "axios";

const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: import.meta.env.VITE_API_URL,
    timeout: 10000,
  });

  // Request interceptor
  client.interceptors.request.use((config) => {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  // Response interceptor
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response?.status === 401) {
        // Handle unauthorized
      }
      return Promise.reject(error);
    }
  );

  return client;
};

export const api = createApiClient();
```

## Type Safety

- Use `interface` for object shapes
- Use `type` for unions, intersections, or primitives
- Avoid `any` - use `unknown` if type is truly unknown
- Use strict null checks
- Define API response types

```typescript
// types/User.ts
export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  createdAt: Date;
}

export type UserRole = "admin" | "user" | "guest";

export interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}
```

## Testing

```typescript
import { render, screen, fireEvent } from "@testing-library/react";
import { Component } from "./Component";

describe("Component", () => {
  it("renders title", () => {
    render(<Component title="Test" />);
    expect(screen.getByText("TEST")).toBeInTheDocument();
  });

  it("handles click", () => {
    const handleClick = jest.fn();
    render(<Component title="Test" onClick={handleClick} />);

    fireEvent.click(screen.getByText("TEST"));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });
});
```

## Environment Variables

- Prefix with `VITE_` for exposure to client
- Validate at runtime in `main.tsx`
- Use TypeScript for type safety

```typescript
// config/env.ts
interface EnvConfig {
  apiUrl: string;
  cognitoUserPoolId: string;
  cognitoClientId: string;
}

export const env: EnvConfig = {
  apiUrl: import.meta.env.VITE_API_URL,
  cognitoUserPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID,
  cognitoClientId: import.meta.env.VITE_COGNITO_CLIENT_ID,
};

// Validate
Object.entries(env).forEach(([key, value]) => {
  if (!value) {
    throw new Error(`Missing environment variable: ${key}`);
  }
});
```

## Error Boundaries

```typescript
import React from "react";

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  state: State = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error("Error caught by boundary:", error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return <div>Something went wrong. Please refresh.</div>;
    }
    return this.props.children;
  }
}
```

## Routing (React Router v6)

```typescript
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { useStore } from "./stores";

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = ({
  children,
}) => {
  const { authStore } = useStore();
  return authStore.isAuthenticated ? children : <Navigate to="/login" />;
};

export const App: React.FC = observer(() => (
  <BrowserRouter>
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/dashboard" />} />
    </Routes>
  </BrowserRouter>
));
```

## Performance

- Use `React.memo()` for expensive renders
- Use `useMemo()` for expensive computations
- Use `useCallback()` for stable function references
- Lazy load routes with `React.lazy()`
- Virtualize long lists

## Avoid

- ❌ Mutating state directly
- ❌ Using `any` type
- ❌ Large components (split them)
- ❌ Inline styles (use Tailwind classes)
- ❌ Logic in JSX (extract to functions)
- ❌ Multiple useState for related data (use useReducer or MobX)

## Debugging Frontend Issues

### Core Principle

**Check backend infrastructure and configuration before debugging frontend code.**

### When Frontend Fails But Backend "Should" Work

Follow this order:

1. **Check Backend Infrastructure** (Are APIs deployed and healthy?)
2. **Check Configuration** (Is API URL correct? CORS configured?)
3. **Check Frontend** (Is there a code bug?)

### Common Scenarios

#### 1. CORS Errors

**Symptom**: `Access to XMLHttpRequest ... has been blocked by CORS policy`

**Debug Steps**:

```bash
# Layer 1: Infrastructure - Check if API Gateway is deployed
aws apprunner list-services --region us-east-1

# Layer 2: Configuration - Check CORS configuration in Terraform
cat terraform/deploy/modules/app-runner/main.tf | grep -A 5 "Cors__AllowedOrigins"

# Common issues:
# - Origin missing https:// protocol
# - Origin not in allowed list
# - Wildcard (*) not working as expected

# Layer 3: Application - Check frontend API configuration
cat frontend/src/config/api.ts
```

**CORS Checklist**:

- [ ] Origin includes `https://` protocol
- [ ] Origin matches exactly (no trailing slash)
- [ ] Backend CORS middleware is configured
- [ ] OPTIONS preflight requests succeed
- [ ] `Access-Control-Allow-Origin` header is present in response

**Common Fix**:

```hcl
# terraform/deploy/modules/app-runner/main.tf
# Add CloudFront origin with protocol
Cors__AllowedOrigins__10 = "https://${var.cloudfront_distribution_domain}"
```

#### 2. API Returns 404 Not Found

**Symptom**: `GET https://api.example.com/api/v1/resource 404`

**Debug Steps**:

```bash
# 1. Verify API Gateway is deployed and healthy
aws apprunner describe-service --service-arn <api-gateway-arn>

# 2. Check API Gateway URL in frontend config
cat frontend/src/config/api.ts

# 3. Verify endpoint exists in backend
# Review backend controllers for the route

# 4. Test endpoint directly (bypass frontend)
curl https://api-gateway-url/api/v1/resource
```

**Common Issues**:

- Wrong base URL in frontend config
- Route doesn't exist in backend
- API Gateway not routing to correct service
- Version mismatch (`/api/v1/` vs `/api/v2/`)

#### 3. API Returns 500 Internal Server Error

**Symptom**: `GET https://api.example.com/api/v1/resource 500`

**This is NOT a frontend issue!**

**Debug Steps**:

```bash
# 1. Check backend logs (infrastructure)
aws logs tail /aws/apprunner/navarch-studio-dev-data-service/service --since 5m

# 2. If no logs, observability not configured (infrastructure issue)

# 3. If logs exist, find the exception
aws logs tail /aws/apprunner/navarch-studio-dev-data-service/service --since 5m | grep -i exception

# 4. Fix backend issue (not frontend)
```

**Do NOT**:

- ❌ Add retry logic in frontend
- ❌ Modify API call in frontend
- ❌ Add error handling in frontend
- ✅ Fix the backend issue first

#### 4. Authentication Fails

**Symptom**: Cognito login fails or token validation fails

**Debug Steps**:

```typescript
// 1. Check Cognito configuration in environment
console.log('Cognito User Pool ID:', import.meta.env.VITE_COGNITO_USER_POOL_ID);
console.log('Cognito Client ID:', import.meta.env.VITE_COGNITO_CLIENT_ID);

// 2. Verify environment variables are set
cat frontend/.env.local

// 3. Check if token is being sent correctly
// In browser DevTools → Network → Headers
// Should have: Authorization: Bearer <token>

// 4. Verify backend Cognito configuration matches frontend
cat terraform/deploy/modules/app-runner/main.tf | grep Cognito
```

**Common Issues**:

- User Pool ID or Client ID mismatch
- Token expired (check expiration time)
- Token not included in request headers
- Backend Cognito config doesn't match frontend

#### 5. Timeout Errors

**Symptom**: `timeout of 30000ms exceeded`

**Debug Steps**:

```typescript
// 1. Check if backend is responding at all
curl https://api-gateway-url/health

// 2. If health check fails, backend is down (infrastructure)

// 3. If health check succeeds but endpoint times out:
// - Backend might be processing too slowly
// - Database query taking too long
// - Network issue

// 4. Check backend logs for slow operations
aws logs tail /aws/apprunner/<service>/service --since 5m | grep -i timeout
```

**Timeout Decision Tree**:

- Health check fails → Infrastructure (backend down)
- All endpoints timeout → Infrastructure (backend overloaded)
- Specific endpoint times out → Application (backend code issue)
- Intermittent timeouts → Infrastructure (scaling issue)

#### 6. Data Not Loading / Empty State

**Symptom**: UI shows empty state but data should exist

**Debug Steps**:

```typescript
// 1. Check if API call is being made
// Browser DevTools → Network tab
// Look for the API request

// 2. Check API response
// Click on request → Preview/Response tab
// Is data actually being returned?

// 3. If no data, check backend database
// This is a backend issue, not frontend

// 4. If data is returned, check frontend data mapping
// Review MobX store or component state
// Check if data transformation is correct
```

### Browser DevTools Debugging

**Network Tab Checklist**:

```
For each failed request, check:
1. Status Code
   - 404 → Endpoint doesn't exist (check backend routes)
   - 500 → Backend error (check backend logs)
   - 401/403 → Auth issue (check token)
   - CORS error → Check CORS configuration

2. Request Headers
   - Authorization header present?
   - Content-Type correct?

3. Response Headers
   - Access-Control-Allow-Origin present?
   - Content-Type correct?

4. Response Body
   - Error message?
   - Stack trace?
```

**Console Tab Checklist**:

```
Look for:
1. Errors (red)
   - Network errors → Check backend/CORS
   - Type errors → Frontend code issue
   - Reference errors → Missing imports

2. Warnings (yellow)
   - React warnings → Fix component code
   - Deprecation warnings → Update dependencies

3. Custom logs
   - Store actions → Check MobX state
   - API responses → Check data structure
```

### Environment Variables Debugging

**Symptom**: `undefined` environment variables

**Debug Steps**:

```bash
# 1. Verify .env.local exists and has VITE_ prefix
cat frontend/.env.local

# Example:
VITE_API_URL=https://api-gateway-url
VITE_COGNITO_USER_POOL_ID=us-east-1_xxxxx
VITE_COGNITO_CLIENT_ID=xxxxx

# 2. Restart dev server (env vars only loaded on startup)
npm run dev

# 3. Verify in browser console
console.log(import.meta.env.VITE_API_URL);
```

**Common Issues**:

- Missing `VITE_` prefix
- Dev server not restarted after changing .env
- .env.local not created (only .env.example exists)

### API Configuration

**Best Practice**:

```typescript
// config/api.ts
export const API_CONFIG = {
  baseURL: import.meta.env.VITE_API_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
};

// Validate on app startup
if (!API_CONFIG.baseURL) {
  throw new Error("VITE_API_URL environment variable not set");
}

console.log("✅ API configured:", API_CONFIG.baseURL);
```

### MobX Store Debugging

**Common Issues**:

```typescript
// ❌ Bad: Not using runInAction for async updates
async fetchData() {
  const response = await api.get('/data');
  this.data = response.data; // Warning: should be in runInAction
}

// ✅ Good: Use runInAction
async fetchData() {
  const response = await api.get('/data');
  runInAction(() => {
    this.data = response.data;
  });
}

// ✅ Good: Log state changes for debugging
async fetchData() {
  console.log('📥 Fetching data...');
  const response = await api.get('/data');
  runInAction(() => {
    this.data = response.data;
    console.log('✅ Data loaded:', this.data.length, 'items');
  });
}
```

### Debugging Checklist

Before debugging frontend code:

- [ ] Verify backend APIs are deployed and healthy
- [ ] Check API Gateway URL in frontend config
- [ ] Verify CORS is configured correctly
- [ ] Check authentication token is being sent
- [ ] Verify environment variables are set
- [ ] Review Network tab for failed requests
- [ ] Check Console for errors
- [ ] Only then debug frontend code

### Common Mistakes

❌ **Assuming backend is working** - Always verify APIs are accessible
❌ **Not checking Network tab** - It shows exactly what failed
❌ **Debugging frontend first** - Check backend infrastructure first
❌ **Ignoring CORS errors** - Fix CORS configuration, not frontend code
❌ **Not logging API responses** - Makes debugging much harder

### Best Practice Workflow

1. **Issue Reported** → Check browser DevTools Network tab
2. **CORS Error** → Check Terraform CORS configuration
3. **404 Error** → Verify endpoint exists in backend
4. **500 Error** → Check backend logs (infrastructure issue)
5. **Auth Error** → Verify Cognito configuration matches
6. **Timeout** → Check if backend is responding
7. **Only Then** → Debug frontend code

### Logging Best Practices

```typescript
// ✅ Good: Log API calls with context
export const api = {
  async get<T>(url: string): Promise<T> {
    console.log(`📥 GET ${url}`);
    try {
      const response = await axiosInstance.get<T>(url);
      console.log(`✅ GET ${url} - Success`);
      return response.data;
    } catch (error) {
      console.error(`❌ GET ${url} - Failed:`, error);
      throw error;
    }
  },
};

// ✅ Good: Log store actions
@action
async loadVessels() {
  console.log('🚢 Loading vessels...');
  this.loading = true;
  try {
    const vessels = await api.getVessels();
    runInAction(() => {
      this.vessels = vessels;
      console.log(`✅ Loaded ${vessels.length} vessels`);
    });
  } catch (error) {
    runInAction(() => {
      this.error = error.message;
      console.error('❌ Failed to load vessels:', error);
    });
  } finally {
    runInAction(() => {
      this.loading = false;
    });
  }
}
```

### Cross-References

- [Full Debugging Methodology](./debugging-methodology.md)
- [Troubleshooting Flowchart](./troubleshooting-flowchart.md)
- [.NET Cloud Debugging](./dotnet.md#debugging-net-applications-in-cloud)
- [Terraform Debugging](./terraform.md#debugging-terraform-managed-infrastructure)
