import { CognitoUserPool } from "amazon-cognito-identity-js";
import { getConfig, isConfigLoaded } from "./runtime";

let _userPool: CognitoUserPool | null = null;

/**
 * Get Cognito UserPool instance (lazy-loaded)
 * Uses runtime config if available, otherwise falls back to environment variables
 */
export const getUserPool = (): CognitoUserPool | null => {
  if (_userPool) {
    return _userPool;
  }

  const authMode = isConfigLoaded()
    ? getConfig().authMode
    : import.meta.env.VITE_AUTH_MODE || "local";

  if (authMode !== "cognito") {
    return null; // Local mode
  }

  const poolData = isConfigLoaded()
    ? {
        UserPoolId: getConfig().cognitoUserPoolId,
        ClientId: getConfig().cognitoClientId,
      }
    : {
        UserPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID || "dummy-pool-id",
        ClientId: import.meta.env.VITE_COGNITO_CLIENT_ID || "dummy-client-id",
      };

  _userPool = new CognitoUserPool(poolData);
  return _userPool;
};

// For backward compatibility with code that uses userPool directly
// Note: This will be null initially and should use getUserPool() instead
export const userPool = null;

export const getCognitoConfig = () => {
  if (isConfigLoaded()) {
    const config = getConfig();
    return {
      userPoolId: config.cognitoUserPoolId,
      clientId: config.cognitoClientId,
      region: config.awsRegion,
    };
  }

  return {
    userPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID,
    clientId: import.meta.env.VITE_COGNITO_CLIENT_ID,
    region: import.meta.env.VITE_AWS_REGION || "us-east-1",
  };
};
