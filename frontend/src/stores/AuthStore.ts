import { makeAutoObservable, runInAction } from "mobx";
import {
  CognitoUser,
  CognitoUserAttribute,
  AuthenticationDetails,
  CognitoUserSession,
} from "amazon-cognito-identity-js";
import { userPool } from "../config/cognito";
import { LocalAuthService } from "../services/localAuthService";

export interface User {
  id: string;
  email: string;
  name: string;
  preferredUnits?: string;
}

type AuthMode = 'cognito' | 'local';

export class AuthStore {
  user: User | null = null;
  isAuthenticated = false;
  loading = false;
  error: string | null = null;
  private currentSession: CognitoUserSession | null = null;
  private authMode: AuthMode;

  constructor() {
    makeAutoObservable(this);
    // Determine auth mode from environment
    this.authMode = (import.meta.env.VITE_AUTH_MODE || 'local') as AuthMode;
    this.initializeAuth();
  }

  private initializeAuth(): void {
    if (this.authMode === 'local') {
      // Local JWT auth - check for stored token
      const user = LocalAuthService.getUser();
      if (user && LocalAuthService.isAuthenticated()) {
        runInAction(() => {
          this.user = user;
          this.isAuthenticated = true;
        });
      }
    } else {
      // Cognito auth - check for existing session
      const cognitoUser = userPool.getCurrentUser();
      if (cognitoUser) {
        cognitoUser.getSession((err: Error | null, session: CognitoUserSession | null) => {
          if (err || !session?.isValid()) {
            console.log("No valid session found");
            return;
          }

          cognitoUser.getUserAttributes((err, attributes) => {
            if (err || !attributes) {
              console.log("Failed to get user attributes");
              return;
            }

            const email = attributes.find((attr) => attr.Name === "email")?.Value || "";
            const name = attributes.find((attr) => attr.Name === "name")?.Value || email;

            runInAction(() => {
              this.user = {
                id: session.getIdToken().payload.sub,
                email,
                name,
              };
              this.isAuthenticated = true;
              this.currentSession = session;
            });
          });
        });
      }
    }
  }

  async login(email: string, password: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      if (this.authMode === 'local') {
        // Local JWT auth
        const user = await LocalAuthService.login(email, password);
        
        runInAction(() => {
          this.user = user;
          this.isAuthenticated = true;
          this.loading = false;
        });
        
        // Load user settings including preferred units
        const { settingsStore } = await import("./SettingsStore");
        await settingsStore.loadSettings();
      } else {
        // Cognito auth
        return new Promise((resolve, reject) => {
          const authenticationDetails = new AuthenticationDetails({
            Username: email,
            Password: password,
          });

          const userData = {
            Username: email,
            Pool: userPool,
          };

          const cognitoUser = new CognitoUser(userData);

          cognitoUser.authenticateUser(authenticationDetails, {
            onSuccess: (session: CognitoUserSession) => {
              cognitoUser.getUserAttributes((err, attributes) => {
                if (err) {
                  runInAction(() => {
                    this.error = "Failed to get user attributes";
                    this.loading = false;
                  });
                  reject(err);
                  return;
                }

                const name = attributes?.find((attr) => attr.Name === "name")?.Value || email;

                runInAction(() => {
                  this.user = {
                    id: session.getIdToken().payload.sub,
                    email,
                    name,
                  };
                  this.isAuthenticated = true;
                  this.currentSession = session;
                  this.loading = false;
                });
                resolve();
              });
            },

            onFailure: (err: Error) => {
              runInAction(() => {
                this.error = err.message || "Login failed";
                this.loading = false;
              });
              reject(err);
            },

            newPasswordRequired: () => {
              runInAction(() => {
                this.error = "New password required. Please contact support.";
                this.loading = false;
              });
              reject(new Error("New password required"));
            },
          });
        });
      }
    } catch (err) {
      runInAction(() => {
        this.error = err instanceof Error ? err.message : "Login failed";
        this.loading = false;
      });
      throw err;
    }
  }

  async signup(email: string, password: string, name: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      if (this.authMode === 'local') {
        // Local JWT auth
        await LocalAuthService.signup(email, password, name);
        
        runInAction(() => {
          this.loading = false;
        });
      } else {
        // Cognito auth
        return new Promise((resolve, reject) => {
          const attributeList: CognitoUserAttribute[] = [
            new CognitoUserAttribute({
              Name: "email",
              Value: email,
            }),
            new CognitoUserAttribute({
              Name: "name",
              Value: name,
            }),
          ];

          userPool.signUp(email, password, attributeList, [], (err, _result) => {
            if (err) {
              runInAction(() => {
                this.error = err.message || "Signup failed";
                this.loading = false;
              });
              reject(err);
              return;
            }

            runInAction(() => {
              this.loading = false;
            });
            resolve();
          });
        });
      }
    } catch (err) {
      runInAction(() => {
        this.error = err instanceof Error ? err.message : "Signup failed";
        this.loading = false;
      });
      throw err;
    }
  }

  async confirmSignup(email: string, code: string): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      if (this.authMode === 'local') {
        // Local JWT auth doesn't require email confirmation
        runInAction(() => {
          this.loading = false;
        });
        return Promise.resolve();
      }

      // Cognito auth - requires email confirmation
      return new Promise((resolve, reject) => {
        const cognitoUser = new CognitoUser({
          Username: email,
          Pool: userPool,
        });

        cognitoUser.confirmRegistration(code, true, (err, _result) => {
          if (err) {
            runInAction(() => {
              this.error = err.message || "Verification failed";
              this.loading = false;
            });
            reject(err);
            return;
          }

          runInAction(() => {
            this.loading = false;
          });
          resolve();
        });
      });
    } catch (err) {
      runInAction(() => {
        this.error = err instanceof Error ? err.message : "Verification failed";
        this.loading = false;
      });
      throw err;
    }
  }

  logout(): void {
    if (this.authMode === 'local') {
      LocalAuthService.logout();
    } else {
      const cognitoUser = userPool.getCurrentUser();
      if (cognitoUser) {
        cognitoUser.signOut();
      }
    }

    runInAction(() => {
      this.user = null;
      this.isAuthenticated = false;
      this.currentSession = null;
    });
  }

  async getIdToken(): Promise<string | null> {
    if (this.authMode === 'local') {
      return LocalAuthService.getToken();
    }

    if (!this.currentSession) {
      return null;
    }

    // Check if token needs refresh
    if (!this.currentSession.isValid()) {
      const cognitoUser = userPool.getCurrentUser();
      if (!cognitoUser) {
        return null;
      }

      return new Promise((resolve) => {
        cognitoUser.getSession((err: Error | null, session: CognitoUserSession | null) => {
          if (err || !session) {
            resolve(null);
            return;
          }

          runInAction(() => {
            this.currentSession = session;
          });

          resolve(session.getIdToken().getJwtToken());
        });
      });
    }

    return this.currentSession.getIdToken().getJwtToken();
  }

  clearError(): void {
    this.error = null;
  }
}
