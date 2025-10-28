import { CognitoUserPool } from "amazon-cognito-identity-js";

const authMode = import.meta.env.VITE_AUTH_MODE || "cognito";

const poolData = {
  UserPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID || "dummy-pool-id",
  ClientId: import.meta.env.VITE_COGNITO_CLIENT_ID || "dummy-client-id",
};

// Only initialize CognitoUserPool if in cognito mode
export const userPool = authMode === "cognito" 
  ? new CognitoUserPool(poolData)
  : null as any; // Dummy value for local mode

export const getCognitoConfig = () => ({
  userPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID,
  clientId: import.meta.env.VITE_COGNITO_CLIENT_ID,
  region: import.meta.env.VITE_AWS_REGION || "us-east-1",
});
