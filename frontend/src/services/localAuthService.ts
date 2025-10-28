/**
 * Local JWT-based authentication service for development
 * Uses the IdentityService backend instead of AWS Cognito
 */

import axios from "axios";

export interface LocalAuthUser {
  id: string;
  email: string;
  name: string;
  preferredUnits?: string;
}

export interface LoginResponse {
  token: string;
}

export interface CreateUserResponse {
  id: string;
  email: string;
  name: string;
}

const getBaseUrl = () => {
  return `${import.meta.env.VITE_API_URL || "http://localhost:5002"}/api/v1`;
};

// Create a simple axios instance for auth endpoints (no interceptors to avoid circular deps)
const authAxios = axios.create({
  baseURL: getBaseUrl(),
  timeout: 30000, // 30 seconds - consistent with main API client
});

export class LocalAuthService {
  private static TOKEN_KEY = "jwt_token";
  private static USER_KEY = "user_data";

  /**
   * Login with email and password
   */
  static async login(email: string, password: string): Promise<LocalAuthUser> {
    const response = await authAxios.post<LoginResponse>("/auth/login", { email, password });

    const token = response.data.token;
    this.setToken(token);

    // Fetch user details using the token
    const user = await this.getCurrentUser();
    this.setUser(user);

    return user;
  }

  /**
   * Create a new user account
   */
  static async signup(email: string, password: string, name: string): Promise<void> {
    await authAxios.post<CreateUserResponse>("/users", { email, password, name });
    // Note: User must still login after signup
  }

  /**
   * Get current user from API
   */
  static async getCurrentUser(): Promise<LocalAuthUser> {
    const token = this.getToken();
    if (!token) {
      throw new Error("No authentication token found");
    }

    const response = await authAxios.get<LocalAuthUser>("/users/me", {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    return response.data;
  }

  /**
   * Logout - clear local storage
   */
  static logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  /**
   * Get stored JWT token
   */
  static getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Store JWT token
   */
  static setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  /**
   * Get stored user data
   */
  static getUser(): LocalAuthUser | null {
    const userData = localStorage.getItem(this.USER_KEY);
    if (!userData) return null;

    try {
      return JSON.parse(userData);
    } catch {
      return null;
    }
  }

  /**
   * Store user data
   */
  static setUser(user: LocalAuthUser): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  /**
   * Check if user is authenticated
   */
  static isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
