# Runtime Configuration System

## Overview

The NavArch Studio frontend uses a **runtime configuration** system that allows backend URLs and settings to be updated without rebuilding the frontend application. This prevents issues where backend services are recreated with new URLs, breaking the frontend.

## How It Works

### 1. Configuration File Generation (Terraform)

When you run `terraform apply`, a `config.json` file is automatically generated and uploaded to S3:

```hcl
# terraform/deploy/modules/s3-cloudfront/main.tf
resource "aws_s3_object" "config" {
  bucket       = aws_s3_bucket.frontend.id
  key          = "config.json"
  content_type = "application/json"
  
  content = jsonencode({
    apiUrl            = var.api_gateway_url
    authMode          = "cognito"
    cognitoUserPoolId = var.cognito_user_pool_id
    cognitoClientId   = var.cognito_client_id
    awsRegion         = var.aws_region
  })
  
  cache_control = "public, max-age=300"  # 5-minute cache
}
```

**Key Points:**
- Updates automatically when backend URLs change
- Cached for 5 minutes (balances freshness vs performance)
- Uses MD5 etag to detect changes

### 2. Frontend Loading (Runtime)

When the app starts, it fetches `/config.json`:

```typescript
// frontend/src/config/runtime.ts
export async function loadConfig(): Promise<AppConfig> {
  const response = await fetch('/config.json', {
    cache: 'no-cache',
  });
  return await response.json();
}
```

**Fallback Strategy:**
1. Try to load `/config.json` (production)
2. Fall back to build-time environment variables (local development)
3. Fall back to `localhost:5002` (default)

### 3. App Initialization

The `ConfigLoader` component loads configuration before rendering the app:

```typescript
// frontend/src/App.tsx
const ConfigLoader: React.FC = ({ children }) => {
  const [configLoaded, setConfigLoaded] = useState(false);

  useEffect(() => {
    loadConfig().then(() => {
      setConfigLoaded(true);
    });
  }, []);

  if (!configLoaded) {
    return <div>Loading configuration...</div>;
  }

  return <>{children}</>;
};
```

### 4. API Service Usage

The API service uses the loaded configuration:

```typescript
// frontend/src/services/api.ts
const createApiClient = (): AxiosInstance => {
  const apiUrl = isConfigLoaded()
    ? getConfig().apiUrl
    : import.meta.env.VITE_API_URL || "http://localhost:5002";
  
  const baseURL = `${apiUrl}/api/v1`;
  // ...
}
```

## Benefits

### Before (Build-time Configuration)

❌ **URL changes required frontend rebuild:**
```
Services recreated with new URLs
   ↓
Frontend has old URL baked in JavaScript
   ↓
Must rebuild & redeploy frontend (20 minutes)
   ↓
Users can access app again
```

### After (Runtime Configuration)

✅ **URL changes handled automatically:**
```
Services recreated with new URLs
   ↓
Terraform updates config.json (1 minute)
   ↓
Frontend fetches new config on next load
   ↓
Users can access app immediately
```

### Comparison

| Aspect | Build-time | Runtime |
|--------|-----------|---------|
| **URL changes** | Requires rebuild | Auto-updates |
| **Deployment time** | 20 minutes | 2 minutes |
| **Frontend/backend coupling** | Tight | Loose |
| **Emergency fixes** | Full deployment | Update S3 file |
| **Testing** | Hard | Easy |
| **Initial load time** | ✅ Fast | ⚠️ +100ms |

## Configuration Schema

```typescript
interface AppConfig {
  apiUrl: string;              // e.g., "https://abc123.awsapprunner.com"
  authMode: 'cognito' | 'local';
  cognitoUserPoolId: string;   // e.g., "us-east-1_ABC123"
  cognitoClientId: string;     // e.g., "1abc2def3ghi..."
  awsRegion: string;           // e.g., "us-east-1"
}
```

## Local Development

### Using Build-time Config (Default)

When `/config.json` doesn't exist (local dev), the system falls back to environment variables:

```bash
# .env
VITE_API_URL=http://localhost:5002
VITE_AUTH_MODE=local
```

### Testing Runtime Config Locally

Create a mock `config.json`:

```bash
# In frontend/public/config.json
{
  "apiUrl": "http://localhost:5002",
  "authMode": "local",
  "cognitoUserPoolId": "",
  "cognitoClientId": "",
  "awsRegion": "us-east-1"
}
```

## Deployment

### Automatic Deployment

When you run `terraform apply`, the config.json is automatically created/updated:

```bash
cd terraform/deploy
terraform apply

# Outputs show current configuration:
# api_gateway_url = "https://abc123.awsapprunner.com"
#
# config.json is automatically generated with these values
```

### Manual Update (Emergency)

If you need to update config without Terraform:

```bash
# Create new config
cat > config.json << EOF
{
  "apiUrl": "https://NEW-URL.awsapprunner.com",
  "authMode": "cognito",
  "cognitoUserPoolId": "us-east-1_ABC123",
  "cognitoClientId": "client-id",
  "awsRegion": "us-east-1"
}
EOF

# Upload to S3
aws s3 cp config.json s3://navarch-studio-dev-frontend/config.json \
  --content-type "application/json" \
  --cache-control "public, max-age=300"

# Invalidate CloudFront cache
aws cloudfront create-invalidation \
  --distribution-id E1234567890ABC \
  --paths "/config.json"
```

## Troubleshooting

### Config Not Loading

**Symptom:** App shows "Loading configuration..." forever

**Causes:**
1. `/config.json` returns 404
2. CORS issue (shouldn't happen with CloudFront)
3. Network error

**Solution:**
```bash
# Check if config.json exists in S3
aws s3 ls s3://navarch-studio-dev-frontend/config.json

# Check CloudFront URL
curl https://YOUR-CLOUDFRONT-DOMAIN.cloudfront.net/config.json

# View browser console for errors
# Look for: "[Config] ⚠️ Failed to load runtime config"
```

### Old URL Still Being Used

**Symptom:** App calls old API Gateway URL even after Terraform update

**Causes:**
1. CloudFront cache not cleared
2. Browser cache
3. config.json not updated

**Solution:**
```bash
# 1. Check current config.json
curl https://YOUR-CLOUDFRONT-DOMAIN.cloudfront.net/config.json

# 2. If wrong, invalidate CloudFront
aws cloudfront create-invalidation \
  --distribution-id YOUR-DIST-ID \
  --paths "/config.json"

# 3. Hard refresh browser (Ctrl+Shift+R)

# 4. Or use Incognito mode
```

### Fallback Configuration Used

**Symptom:** Console shows `[Config] Using fallback configuration`

**This is expected in these scenarios:**
- ✅ Local development (no S3/CloudFront)
- ✅ First deployment (config.json not created yet)
- ⚠️ Production (config.json failed to load - investigate!)

## Monitoring

### Check Config Loading

Open browser console (F12) and look for:

```
[Config] Loading runtime configuration from /config.json...
[Config] ✅ Runtime configuration loaded successfully
[Config] API URL: https://abc123.awsapprunner.com
[Config] Auth Mode: cognito
```

### Verify Current Config

```typescript
// In browser console:
fetch('/config.json')
  .then(r => r.json())
  .then(config => console.log(config));

// Should show:
// {
//   apiUrl: "https://abc123.awsapprunner.com",
//   authMode: "cognito",
//   ...
// }
```

## Migration Notes

### Existing Deployments

If you're updating an existing deployment:

1. **Frontend will still work** during transition (uses fallback)
2. **After Terraform apply**, config.json is created
3. **On next browser refresh**, runtime config is used
4. **No downtime** during migration

### Backward Compatibility

The system is fully backward compatible:

```typescript
// If config.json doesn't exist, falls back to:
const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5002";
```

Existing deployments continue working until config.json is created.

## API Reference

### `loadConfig(): Promise<AppConfig>`

Loads configuration from `/config.json`. Caches result to avoid multiple fetches.

```typescript
import { loadConfig } from './config/runtime';

const config = await loadConfig();
console.log(config.apiUrl);
```

### `getConfig(): AppConfig`

Returns cached configuration synchronously. Throws if config not loaded yet.

```typescript
import { getConfig, isConfigLoaded } from './config/runtime';

if (isConfigLoaded()) {
  const config = getConfig();
  console.log(config.apiUrl);
}
```

### `isConfigLoaded(): boolean`

Checks if configuration has been loaded.

```typescript
import { isConfigLoaded } from './config/runtime';

if (isConfigLoaded()) {
  // Config is ready
} else {
  // Must call loadConfig() first
}
```

### `clearConfigCache(): void`

Clears cached configuration (useful for testing).

```typescript
import { clearConfigCache, loadConfig } from './config/runtime';

clearConfigCache();
await loadConfig(); // Fetches fresh config
```

## Best Practices

### 1. Always Load Config in App Initialization

✅ **Good:**
```typescript
// App.tsx wraps everything in ConfigLoader
<ConfigLoader>
  <YourApp />
</ConfigLoader>
```

❌ **Bad:**
```typescript
// Trying to use config before loading
const api = createApiClient(); // May not have config yet!
```

### 2. Use Fallbacks for Local Development

✅ **Good:**
```typescript
const apiUrl = isConfigLoaded()
  ? getConfig().apiUrl
  : (import.meta.env.VITE_API_URL || "http://localhost:5002");
```

❌ **Bad:**
```typescript
const apiUrl = getConfig().apiUrl; // Breaks if config not loaded!
```

### 3. Keep Cache TTL Short

✅ **Good:**
```hcl
cache_control = "public, max-age=300"  # 5 minutes
```

❌ **Bad:**
```hcl
cache_control = "public, max-age=86400"  # 24 hours - config changes take too long
```

### 4. Invalidate Config on Infrastructure Changes

```bash
# After terraform apply
aws cloudfront create-invalidation \
  --distribution-id $CF_DIST_ID \
  --paths "/config.json"
```

## Future Enhancements

Potential improvements to consider:

1. **Version tracking** - Add version field to detect stale configs
2. **Retry logic** - Auto-retry failed config loads
3. **Config validation** - Validate config schema on load
4. **Metrics** - Track config load times and failures
5. **Feature flags** - Add feature toggles to runtime config

## Summary

**Runtime configuration eliminates the #1 cause of frontend/backend mismatches:**

- ✅ Backend URLs can change without breaking frontend
- ✅ Faster deployments (no frontend rebuild needed)
- ✅ Easier testing and debugging
- ✅ Better separation of concerns
- ✅ Emergency config updates possible

**The small cost (~100ms initial load) is well worth the flexibility and reliability gains.**

