import { makeAutoObservable, runInAction } from "mobx";
import {
  CognitoUser,
  CognitoUserAttribute,
  AuthenticationDetails,
  CognitoUserSession,
} from "amazon-cognito-identity-js";
import { userPool } from "../config/cognito";

export interface User {
  id: string;
  email: string;
  name: string;
}

export class AuthStore {
  user: User | null = null;
  isAuthenticated = false;
  loading = false;
  error: string | null = null;
  private currentSession: CognitoUserSession | null = null;

  constructor() {
    makeAutoObservable(this);
    this.initializeAuth();
  }

  private initializeAuth(): void {
    // Check for existing session
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

  async login(email: string, password: string): Promise<void> {
    this.loading = true;
    this.error = null;

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

  async signup(email: string, password: string, name: string): Promise<void> {
    this.loading = true;
    this.error = null;

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

  async confirmSignup(email: string, code: string): Promise<void> {
    this.loading = true;
    this.error = null;

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
  }

  logout(): void {
    const cognitoUser = userPool.getCurrentUser();
    if (cognitoUser) {
      cognitoUser.signOut();
    }

    runInAction(() => {
      this.user = null;
      this.isAuthenticated = false;
      this.currentSession = null;
    });
  }

  async getIdToken(): Promise<string | null> {
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





