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
import React from 'react';
import { observer } from 'mobx-react-lite';

interface ComponentProps {
  // Props with explicit types
  title: string;
  onClick?: () => void;
}

export const Component: React.FC<ComponentProps> = observer(({ title, onClick }) => {
  // 1. Hooks
  const [state, setState] = React.useState<string>('');
  
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
  return (
    <div onClick={handleClick}>
      {formattedTitle}
    </div>
  );
});

Component.displayName = 'Component';
```

## MobX Store Pattern

```typescript
import { makeAutoObservable, runInAction } from 'mobx';

export class UserStore {
  users: User[] = [];
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  // Computed
  get activeUsers(): User[] {
    return this.users.filter(u => u.isActive);
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
import axios, { AxiosInstance } from 'axios';

const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: import.meta.env.VITE_API_URL,
    timeout: 10000,
  });

  // Request interceptor
  client.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
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

export type UserRole = 'admin' | 'user' | 'guest';

export interface ApiResponse<T> {
  data: T;
  message: string;
  success: boolean;
}
```

## Testing

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { Component } from './Component';

describe('Component', () => {
  it('renders title', () => {
    render(<Component title="Test" />);
    expect(screen.getByText('TEST')).toBeInTheDocument();
  });

  it('handles click', () => {
    const handleClick = jest.fn();
    render(<Component title="Test" onClick={handleClick} />);
    
    fireEvent.click(screen.getByText('TEST'));
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
import React from 'react';

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
    console.error('Error caught by boundary:', error, errorInfo);
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
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { observer } from 'mobx-react-lite';
import { useStore } from './stores';

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = ({ children }) => {
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






