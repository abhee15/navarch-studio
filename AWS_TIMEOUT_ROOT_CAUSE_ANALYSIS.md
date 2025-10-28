# AWS Timeout Root Cause Analysis

## Observed Symptoms

From the browser console logs:

```
[API] Making GET request to: https://qqctyzzmz4.us-east-1.awsapprunner.com/api/v1/users/settings
[API] Added auth token to request
[API] Settings endpoint - using default units
[API] Request config: { ... }

[API] Request failed: {
  url: '/users/settings',
  method: 'get',
  status: undefined,
  statusText: undefined,
  data: undefined,
  message: "timeout of 30000ms exceeded"
}
```

## Key Observations

1. ✅ **Frontend reaches API Gateway** - Request is sent to correct URL
2. ✅ **Auth token is present** - JWT is added to request
3. ✅ **Request configuration is correct** - Headers, method, URL all correct
4. ❌ **No response from API Gateway** - Times out after 30 seconds
5. ❌ **status: undefined** - Server never responded at all

## Root Cause Analysis

### The Critical Issue

The `/users/settings` endpoint **should respond immediately** because it:

- Doesn't call any backend services
- Returns a static response: `{ preferredUnits: "SI" }`
- Is defined directly in API Gateway's UsersController

```csharp
[HttpGet("settings")]
public IActionResult GetSettings()
{
    var settings = new UserSettingsDto { PreferredUnits = "SI" };
    return Ok(settings);
}
```

**If this simple endpoint times out, the API Gateway itself is hung or blocked.**

### Likely Root Causes (in order of probability)

#### 1. **JWT Validation Hanging** ⭐ MOST LIKELY

The `JwtAuthenticationMiddleware` runs BEFORE the controller and attempts to validate JWT tokens:

**In Production:**

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IJwtService, LocalJwtService>();
}
else
{
    builder.Services.AddSingleton<IJwtService, CognitoJwtService>();  // ← AWS uses this
}
```

**The CognitoJwtService** needs to:

1. Download JWT signing keys from Cognito (JWKS endpoint)
2. Validate the token signature
3. Verify token expiration

**Why it might hang:**

- ❌ No VPC egress configured → Can't reach Cognito on internet
- ❌ No internet access → Can't download JWKS keys
- ❌ Cognito misconfigured → Invalid endpoints
- ❌ Network timeout → 30 seconds matches our timeout

**Evidence:**

- Both endpoints fail (settings AND vessels)
- Both require authentication (JWT middleware runs first)
- Timeout is exactly 30 seconds (default HTTP timeout)

#### 2. **Missing VPC Egress Configuration**

App Runner services are isolated by default. The API Gateway needs:

- **VPC egress** to reach other App Runner services (IdentityService, DataService)
- **Internet access** to reach AWS Cognito for JWT validation

**Without VPC egress:**

- ✅ Frontend can reach API Gateway (it's publicly accessible)
- ❌ API Gateway cannot reach Cognito (no internet)
- ❌ API Gateway cannot reach IdentityService/DataService (no VPC)

#### 3. **API Gateway Service Not Healthy**

Possible the service is running but not healthy:

- Out of memory
- Crashed but App Runner hasn't restarted it yet
- Deadlocked in initialization
- Database connection pool exhausted

#### 4. **Rate Limiting Blocking All Requests**

The API Gateway has rate limiting configured. Might be:

- Rate limit too aggressive
- All requests blocked
- But unlikely since timeout is 30s, not instant rejection

## Diagnostic Commands

### Step 1: Check API Gateway Health

```bash
# Check if API Gateway health endpoint works
curl -v https://qqctyzzmz4.us-east-1.awsapprunner.com/health
```

**Expected:** Should return 200 OK immediately (no auth required)
**If timeout:** API Gateway service is completely unresponsive

### Step 2: Check VPC Egress Configuration

```bash
# Get API Gateway service ARN
aws apprunner list-services --region us-east-1 \
  --query "ServiceSummaryList[?ServiceUrl=='https://qqctyzzmz4.us-east-1.awsapprunner.com'].[ServiceArn]" \
  --output text

# Check network configuration (use ARN from above)
aws apprunner describe-service \
  --service-arn "arn:aws:apprunner:us-east-1:ACCOUNT:service/sri-template-api-gateway/SERVICE_ID" \
  --region us-east-1 \
  --query 'Service.NetworkConfiguration.EgressConfiguration' \
  --output json
```

**Expected Output:**

```json
{
  "EgressType": "VPC",
  "VpcConnectorArn": "arn:aws:apprunner:..."
}
```

**If you see:**

```json
{
  "EgressType": "DEFAULT"
}
```

→ **THIS IS THE PROBLEM** - No VPC egress means no internet access

### Step 3: Check Cognito Configuration

```bash
# Check environment variables
aws apprunner describe-service \
  --service-arn "SERVICE_ARN" \
  --region us-east-1 \
  --query 'Service.SourceConfiguration.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables' \
  --output json
```

**Look for:**

- `AWS__Region` - Should be "us-east-1"
- `AWS__UserPoolId` - Should be your Cognito user pool ID
- `AWS__UserPoolClientId` - Should be your Cognito client ID
- `ASPNETCORE_ENVIRONMENT` - Should be "Production" or "Dev"

**If these are missing or wrong:** Cognito JWT validation will fail

### Step 4: Check CloudWatch Logs

```bash
# Get recent logs from API Gateway
aws logs tail /aws/apprunner/sri-template-api-gateway \
  --since 10m \
  --follow \
  --region us-east-1
```

**Look for:**

- JWT validation errors
- Timeout errors
- Connection refused
- "No such host"
- HTTP client timeouts

### Step 5: Check Service Status

```bash
# Check if service is running
aws apprunner describe-service \
  --service-arn "SERVICE_ARN" \
  --region us-east-1 \
  --query 'Service.[Status,HealthCheckConfiguration]' \
  --output json
```

**Status should be:** `"RUNNING"`
**If:** `"OPERATION_IN_PROGRESS"` or `"PAUSED"` → Service not ready

## The Fix

### If VPC Egress is Missing (MOST LIKELY):

The API Gateway MUST have VPC egress to:

1. Reach AWS Cognito on the internet (JWT validation)
2. Reach other App Runner services (IdentityService, DataService)

**Fix via Terraform:**

```hcl
# In terraform/deploy/modules/app-runner-service/main.tf
resource "aws_apprunner_service" "main" {
  # ... existing config ...

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = var.vpc_connector_arn
    }
  }
}
```

**Fix manually (temporary):**

1. Go to AWS Console → App Runner
2. Select "sri-template-api-gateway" service
3. Actions → Update service
4. Networking → Egress: Select "VPC"
5. Choose the VPC connector
6. Deploy

### If Cognito Configuration is Wrong:

Update environment variables in App Runner:

- Verify user pool ID is correct
- Verify region matches
- Check client ID

### If Service is Unhealthy:

Force restart:

```bash
aws apprunner pause-service --service-arn "SERVICE_ARN" --region us-east-1
aws apprunner resume-service --service-arn "SERVICE_ARN" --region us-east-1
```

## Immediate Actions Required

Run these commands and share the output:

1. **Test health endpoint:**

   ```bash
   curl -v https://qqctyzzmz4.us-east-1.awsapprunner.com/health
   ```

2. **Check VPC egress:**

   ```bash
   # Get service ARN first
   aws apprunner list-services --region us-east-1 --output json | grep sri-template-api-gateway -A 5

   # Then check network config
   aws apprunner describe-service --service-arn "YOUR_ARN" --region us-east-1 --query 'Service.NetworkConfiguration'
   ```

3. **Check CloudWatch logs:**
   ```bash
   aws logs tail /aws/apprunner/sri-template-api-gateway --since 5m --region us-east-1
   ```

## Expected Timeline

- **If VPC egress missing:** 5-10 minutes to configure and redeploy
- **If Cognito misconfigured:** Update env vars, redeploy (5 minutes)
- **If service unhealthy:** Restart service (2-3 minutes)

## Next Steps

Please run the diagnostic commands above and share:

1. Health endpoint response (or timeout)
2. VPC egress configuration output
3. CloudWatch logs showing any errors
4. Environment variables (sanitized)

This will definitively identify which of the 4 root causes is the issue.
