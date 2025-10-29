# Error Diagnostics & Troubleshooting

## Overview

The application now includes an **intelligent diagnostic system** that automatically analyzes errors and provides actionable guidance. This makes it much easier to identify and fix issues like the URL mismatch problem we encountered.

## What You'll See Now

### Before (Generic Error)

```
[API] Request failed: {
  status: undefined,
  statusText: undefined,
  message: "Network Error"
}
```

❌ **Not helpful!** Doesn't explain what's wrong or how to fix it.

---

### After (Smart Diagnostics)

```
[API] Request failed: {
  status: undefined,
  statusText: undefined,
  message: "Network Error"
}

🔍 API Diagnostic Report
├─ Timestamp: 2025-10-29T16:30:00.000Z
├─ Config Loaded: ✅
├─ Expected API URL: https://h9ptq3ydur.us-east-1.awsapprunner.com
├─ Attempted URL: https://qqctyzzmz4.us-east-1.awsapprunner.com
├─ ⚠️ URL MISMATCH DETECTED!
└─ Service Reachable: ❌

⚠️ URL Mismatch Detected

📋 Recommended Actions:
Expected API URL: https://h9ptq3ydur.us-east-1.awsapprunner.com
Actually calling: https://qqctyzzmz4.us-east-1.awsapprunner.com

This means the frontend has stale configuration.

💡 Solutions:
  1. Hard refresh browser (Ctrl+Shift+R)
  2. Clear browser cache
  3. Open in Incognito/Private mode
  4. Wait for config.json cache to expire (5 minutes)

💡 URL Mismatch: Frontend is calling a different URL than configured. 
   Hard refresh your browser (Ctrl+Shift+R).
```

✅ **Much better!** Explains exactly what's wrong and how to fix it.

---

## Issue Detection

The diagnostic system automatically detects and explains:

### 1. URL Mismatch 🎯 **NEW!**

**What it detects:**
- Frontend calling a different API URL than configured
- Old URL cached in browser
- Stale configuration

**What you'll see:**
```
⚠️ URL MISMATCH DETECTED!
Expected API URL: https://NEW-URL.awsapprunner.com
Actually calling: https://OLD-URL.awsapprunner.com

💡 Solutions:
  1. Hard refresh browser (Ctrl+Shift+R)
  2. Clear browser cache
  3. Open in Incognito/Private mode
```

**This would have immediately identified the issue we faced!**

---

### 2. CORS Errors 🚫

**What it detects:**
- CORS policy blocking requests
- API Gateway not configured for CloudFront domain
- Service stuck in deployment

**What you'll see:**
```
🚫 CORS Error Detected

The API Gateway is not allowing requests from your domain.
This usually means:
  1. API Gateway service is stuck or not fully deployed
  2. CORS configuration has not been applied
  3. CloudFront domain not in allowed origins list

💡 Check service status:
  aws apprunner list-services --region us-east-1

💡 Look for Status: OPERATION_IN_PROGRESS (stuck deployment)

💡 CORS Issue Detected: This usually means the API Gateway service 
   is stuck in deployment or CORS is not configured for your domain.
   Check service status: aws apprunner list-services --region us-east-1
```

---

### 3. Service Unreachable 🔴

**What it detects:**
- API service not responding
- Service down or restarting
- DNS not resolved yet

**What you'll see:**
```
🔴 API Service Not Reachable
Cannot connect to: https://api-url.awsapprunner.com

Possible causes:
  1. Service is down or restarting
  2. Service stuck in deployment (check AWS console)
  3. Network connectivity issues
  4. DNS not resolved yet (new service)

💡 Check service health:
  curl https://api-url.awsapprunner.com/health

💡 Check service status in AWS:
  aws apprunner describe-service --service-arn <ARN>

💡 Service Unreachable: The API service is not responding. 
   It may be down, restarting, or stuck in deployment.
   Check CloudWatch logs or AWS Console for service status.
```

---

### 4. Request Timeout ⏱️

**What it detects:**
- Request taking too long
- Service slow to respond
- Database queries taking time

**What you'll see:**
```
⏱️ Request Timeout
Request took too long to complete.

Possible causes:
  1. API service is processing but slow
  2. Service is under heavy load
  3. Database queries taking too long
  4. Service starting up (migrations running)

💡 Check CloudWatch logs:
  aws logs tail /aws/apprunner/navarch-studio-dev-data-service/service --follow
```

---

## Interactive Health Check

You can now run diagnostics anytime from the browser console:

### 1. Automatic on Load

When the app loads, it automatically runs a health check:

```
🏥 System Health Check
├─ Runtime Config: ✅ Loaded
├─ API URL: https://h9ptq3ydur.us-east-1.awsapprunner.com
├─ Auth Mode: cognito
├─ AWS Region: us-east-1
│
Testing API connectivity...
└─ ✅ API is reachable and responding

💡 Run checkHealth() in console anytime to diagnose issues
```

### 2. Manual Check

Open browser console (F12) and run:

```javascript
checkHealth()
```

**Output:**
```
🏥 System Health Check
├─ Runtime Config: ✅ Loaded
├─ API URL: https://current-url.awsapprunner.com
├─ Auth Mode: cognito
│
Testing API connectivity...
└─ ❌ API is not reachable

💡 Possible issues:
  - Service is down or restarting
  - Service stuck in deployment
  - DNS not resolved yet
  - Network connectivity issues
```

---

## User-Friendly Error Messages

Errors now include user-friendly messages for display in the UI:

### CORS Error
```
🚫 Connection Blocked: The API service is not accepting requests 
from this domain. This usually means the service is being deployed 
or has a configuration issue. Please try again in a few minutes.
```

### Network Error
```
🔴 Cannot Reach API: Unable to connect to the backend service. 
The service may be restarting or experiencing issues. 
Please try again in a moment.
```

### Timeout
```
⏱️ Request Timeout: The request took too long to complete. 
The service may be starting up or under heavy load. 
Please try again.
```

### Server Error (500)
```
⚠️ Server Error: The backend encountered an error processing 
your request. This has been logged and will be investigated.
```

### Authentication (401/403)
```
🔒 Authentication Required: Your session may have expired. 
Please log in again.
```

---

## Example: How It Would Have Helped

### The URL Mismatch Issue We Faced

**What happened:**
- Backend services recreated with new URLs
- Frontend still calling old URLs
- Generic "Network Error" wasn't helpful

**With new diagnostics, you would have seen:**

```javascript
🔍 API Diagnostic Report

⚠️ URL MISMATCH DETECTED!

Expected API URL: https://h9ptq3ydur.us-east-1.awsapprunner.com
Actually calling: https://qqctyzzmz4.us-east-1.awsapprunner.com

This means the frontend has stale configuration.

💡 Solutions:
  1. Hard refresh browser (Ctrl+Shift+R)
  2. Clear browser cache
  3. Open in Incognito/Private mode
  4. Wait for config.json cache to expire (5 minutes)

🔴 Service Not Reachable
Cannot connect to: https://qqctyzzmz4.us-east-1.awsapprunner.com
This URL is no longer valid (service was recreated).
```

**Instantly clear what the problem was!** ✨

---

### The CORS Issue We're Facing Now

**What's happening:**
- API Gateway stuck in deployment
- CORS not applied

**With diagnostics, you see:**

```javascript
🔍 API Diagnostic Report

🚫 CORS ERROR DETECTED

The API Gateway is not allowing requests from your domain.
CloudFront domain not in allowed origins list.

💡 Check service status:
  aws apprunner list-services --region us-east-1

💡 Look for Status: OPERATION_IN_PROGRESS (stuck deployment)

Service may be stuck - consider deleting and recreating:
  aws apprunner delete-service --service-arn <ARN>
```

**Immediately points to the stuck service issue!** ✨

---

## How to Use

### 1. On Every Request

Diagnostics run automatically on every failed API request. Just open the console (F12) to see them.

### 2. Manual Health Check

```javascript
// In browser console (F12):
checkHealth()
```

### 3. Check Configuration

```javascript
// See current config:
fetch('/config.json').then(r => r.json()).then(console.log)
```

---

## Benefits

### For Developers

✅ **Faster debugging** - Clear error messages
✅ **Actionable guidance** - Specific steps to fix issues
✅ **Root cause identification** - Distinguishes between URL mismatch, CORS, service down, etc.
✅ **Easy diagnosis** - `checkHealth()` command available anytime

### For Operations

✅ **Better monitoring** - Clear indication of what's failing
✅ **Proactive detection** - Catches issues like stuck deployments
✅ **Reduced MTTR** - Mean Time To Resolution decreased with clear guidance

### For End Users

✅ **Better error messages** - User-friendly explanations instead of technical jargon
✅ **Clear expectations** - "Try again in a few minutes" vs "Network Error"

---

## Future Enhancements

Potential additions:

1. **Automatic retry** - If service unreachable, retry after delay
2. **Health dashboard** - Visual indicator of service status
3. **Error reporting** - Send diagnostics to monitoring service
4. **Historical tracking** - Track error patterns over time
5. **Notification system** - Alert when critical services down

---

## Summary

The new diagnostic system provides:

🎯 **Intelligent error detection** - Identifies specific issues (URL mismatch, CORS, etc.)
🔍 **Root cause analysis** - Tests service reachability, compares URLs
💡 **Actionable guidance** - Specific commands and steps to fix
🏥 **Health checks** - Manual and automatic system diagnostics
📱 **User-friendly messages** - Clear explanations for non-technical users

**This would have made the URL mismatch issue immediately obvious and provided the exact solution!**

---

## Testing the Diagnostics

After the next deployment completes:

1. **Open browser console** (F12)
2. **Look for:**
   ```
   🏥 System Health Check
   💡 Run checkHealth() in console anytime
   ```
3. **If any error occurs:**
   ```
   🔍 API Diagnostic Report
   [detailed analysis with solutions]
   ```
4. **Try manual check:**
   ```javascript
   checkHealth()
   ```

The diagnostics are now part of every build going forward!
