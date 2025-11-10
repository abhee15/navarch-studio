/**
 * Local JWT-based authentication service for development
 * Uses the IdentityService backend instead of AWS Cognito
 * Includes proactive token expiration checking for better UX
 */

import axios from "axios";
import { jwtDecode } from "jwt-decode";
import { getConfig, isConfigLoaded } from "../config/runtime";
import { getApiUrl } from "../utils/env";

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

interface JwtPayload {
  exp: number;
  sub: string;
  email: string;
  name?: string;
  iat: number;
}

const getBaseUrl = () => {
  const apiUrl = isConfigLoaded() ? getConfig().apiUrl : getApiUrl();
  return `${apiUrl}/api/v1`;
};

// Return a fresh axios instance each time to respect latest runtime config/base URL
const getAuthAxios = () =>
  axios.create({
    baseURL: getBaseUrl(),
    timeout: 30000, // consistent with main API client
  });

export class LocalAuthService {
  private static TOKEN_KEY = "jwt_token";
  private static USER_KEY = "user_data";

  /**
   * Login with email and password
   */
  static async login(email: string, password: string): Promise<LocalAuthUser> {
    const response = await getAuthAxios().post<LoginResponse>("/auth/login", { email, password });

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
    await getAuthAxios().post<CreateUserResponse>("/users", { email, password, name });
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

    const response = await getAuthAxios().get<LocalAuthUser>("/users/me", {
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
    const token = this.getToken();
    if (!token) return false;

    // Also check if token is expired
    return !this.isTokenExpired(token);
  }

  /**
   * Check if a JWT token is expired
   */
  static isTokenExpired(token?: string | null): boolean {
    const tokenToCheck = token || this.getToken();
    if (!tokenToCheck) return true;

    try {
      const decoded = jwtDecode<JwtPayload>(tokenToCheck);
      const currentTime = Date.now() / 1000;

      // Token is expired if current time is past expiration
      return decoded.exp < currentTime;
    } catch (error) {
      console.error("Failed to decode JWT token:", error);
      return true; // Treat invalid tokens as expired
    }
  }

  /**
   * Get time until token expires (in seconds)
   * Returns null if token is invalid or doesn't exist
   */
  static getTimeUntilExpiration(): number | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      const currentTime = Date.now() / 1000;
      const secondsRemaining = Math.max(0, decoded.exp - currentTime);

      return secondsRemaining;
    } catch (error) {
      console.error("Failed to decode JWT token:", error);
      return null;
    }
  }

  /**
   * Get token expiration date
   */
  static getTokenExpiration(): Date | null {
    const token = this.getToken();
    if (!token) return null;

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      return new Date(decoded.exp * 1000);
    } catch (error) {
      console.error("Failed to decode JWT token:", error);
      return null;
    }
  }

  /**
   * Check if token will expire soon (within the given minutes)
   */
  static willExpireSoon(minutes: number = 5): boolean {
    const secondsRemaining = this.getTimeUntilExpiration();
    if (secondsRemaining === null) return true;

    return secondsRemaining < minutes * 60;
  }
}
