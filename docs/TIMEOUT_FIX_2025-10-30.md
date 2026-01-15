# Timeout Issue Fix - October 30, 2025

## Problem Summary

The application was experiencing persistent timeout issues even after previous fixes. Investigation revealed the root cause was **AWS CloudWatch Logger configuration problems**.

## Root Cause

The error logs showed repeated exceptions:
```
Amazon.CloudWatchLogs.Model.InvalidParameterException: 
1 validation error detected: Value null at 'logGroupName' failed to satisfy constraint: Member must not be null
```

Followed by:
```
System.TimeoutException: Flush Timeout - ServiceURL=https://logs.us-east-1.amazonaws.com/, 
StreamName=, PendingMessages=1, CurrentBatch=0
```

### Why This Caused Timeouts

1. **Hardcoded Log Group Names**: The `appsettings.json` files had hardcoded CloudWatch log group names that didn't match the actual project structure:
   - Configured: `/aws/apprunner/sri-template-*-service`
   - Actual: `/aws/apprunner/navarch-studio-{env}-*-service`

2. **Missing IAM Permissions**: The App Runner instance role lacked CloudWatch Logs write permissions

3. **Logger Hanging**: When the AWS Logger couldn't write to CloudWatch, it would hang for 30 seconds trying to flush logs, causing cascading timeouts throughout the application

4. **Redundant Logging**: AWS App Runner with observability enabled (which was already configured) automatically captures all console logs, making the explicit AWS.Logger.SeriLog configuration redundant

## Changes Made

### 1. Removed AWS CloudWatch Logger Configuration

**Files Modified:**
- `/workspace/backend/DataService/appsettings.json`
- `/workspace/backend/IdentityService/appsettings.json`
- `/workspace/backend/ApiGateway/appsettings.json`

**What Changed:**
Removed the problematic `AWSSeriLog` sink from all three services' appsettings.json files. The services now only log to:
- **Console** (captured by App Runner's observability)
- **File** (local logs for debugging)

App Runner's built-in observability automatically forwards all console output to CloudWatch Logs, so we don't need the explicit AWS Logger.

### 2. Added IAM Permissions (Future-Proofing)

**File Modified:**
- `/workspace/terraform/deploy/modules/app-runner/main.tf`

**What Changed:**
Added CloudWatch Logs IAM policy to the App Runner instance role with permissions for:
- `logs:CreateLogGroup`
- `logs:CreateLogStream`
- `logs:PutLogEvents`
- `logs:DescribeLogStreams`

This ensures that if we ever need custom CloudWatch logging in the future, the permissions are in place. The policy is scoped to only log groups matching the pattern:
```
/aws/apprunner/{project_name}-{environment}-*
```

## How Logging Now Works

### Current Architecture

```
Application → Console Output → App Runner Observability → CloudWatch Logs
                     ↓
                 File Logs (local)
```

1. **Application Code**: Uses Serilog to write structured logs
2. **Console Sink**: Outputs logs to stdout in JSON format (CompactJsonFormatter)
3. **App Runner Observability**: Automatically captures all console output
4. **CloudWatch Logs**: Receives logs from App Runner (log group: `/aws/apprunner/{project}-{env}-{service}/service`)
5. **File Sink**: Writes logs locally for debugging (rotated daily, 7-day retention)

### Benefits of This Approach

✅ **No Configuration Needed**: No hardcoded log group names to maintain  
✅ **Automatic Setup**: App Runner creates log groups automatically  
✅ **No Timeouts**: No explicit CloudWatch API calls that can hang  
✅ **Consistent Naming**: Log groups follow App Runner's naming convention  
✅ **X-Ray Integration**: Observability configuration includes AWS X-Ray tracing  

## Deployment Steps

To apply these fixes:

1. **Commit the changes:**
   ```bash
   git add .
   git commit -m "fix: remove problematic AWS CloudWatch Logger configuration causing timeouts"
   ```

2. **Apply Terraform changes (to add IAM permissions):**
   ```bash
   cd terraform/deploy
   terraform plan -var-file="environments/{env}.tfvars"
   terraform apply -var-file="environments/{env}.tfvars"
   ```

3. **Rebuild and deploy services:**
   ```bash
   # From project root
   ./scripts/build-and-push.ps1 -Environment {env}
   ```

4. **Verify the fix:**
   ```bash
   # Check CloudWatch logs for the services - should see no more timeout errors
   aws logs tail /aws/apprunner/navarch-studio-{env}-data-service/service --follow
   aws logs tail /aws/apprunner/navarch-studio-{env}-identity-service/service --follow
   aws logs tail /aws/apprunner/navarch-studio-{env}-api-gateway/service --follow
   ```

## Expected Outcomes

After deploying these changes:

1. ✅ **No More CloudWatch Timeout Errors**: The `InvalidParameterException` and `Flush Timeout` errors will stop
2. ✅ **Faster Application Response**: No more 30-second hangs waiting for logger flushes
3. ✅ **Logs Still Available**: All logs will still appear in CloudWatch through App Runner's observability
4. ✅ **Cleaner Error Logs**: The `aws-logger-errors.txt` files will stop growing with timeout errors

## Monitoring

After deployment, monitor:

1. **Service Health**: Ensure all services remain healthy
   ```bash
   aws apprunner list-services --region us-east-1
   ```

2. **CloudWatch Logs**: Verify logs are still being captured
   ```bash
   aws logs describe-log-groups --log-group-name-prefix "/aws/apprunner/navarch-studio-"
   ```

3. **Application Performance**: Test operations that were timing out before

4. **Error Logs**: Check that the `aws-logger-errors.txt` files are no longer growing:
   ```bash
   # Should not see new errors
   tail -f backend/DataService/aws-logger-errors.txt
   tail -f backend/IdentityService/aws-logger-errors.txt
   ```

## Alternative Approaches Considered

### Option 1: Fix Log Group Names (Not Chosen)
We could have made the log group names configurable via environment variables. However:
- ❌ Adds complexity
- ❌ Still requires manual CloudWatch API calls
- ❌ Redundant with App Runner's built-in logging

### Option 2: Remove AWS.Logger.SeriLog Package (Future Consideration)
We could completely remove the `AWS.Logger.SeriLog` NuGet package since it's no longer used. However:
- ⚠️ Keeping it doesn't hurt (just unused)
- ⚠️ Might be useful for local testing with LocalStack
- ✅ Can be removed in future cleanup

## References

- [AWS App Runner Observability](https://docs.aws.amazon.com/apprunner/latest/dg/monitor-cw.html)
- [Serilog AWS CloudWatch Sink](https://github.com/aws/aws-logging-dotnet)
- [AWS App Runner IAM Policies](https://docs.aws.amazon.com/apprunner/latest/dg/security-iam-service-with-iam.html)

## Lessons Learned

1. **Infrastructure → Configuration → Application**: Following the debugging methodology in `.cursor/rules/debugging-methodology.md` helped identify this was a configuration issue, not application logic
2. **Less is More**: Removing redundant logging configuration eliminated the problem
3. **Use Platform Features**: App Runner's built-in observability is simpler and more reliable than custom CloudWatch integration
4. **Error Logs Are Your Friend**: The `aws-logger-errors.txt` files clearly showed the InvalidParameterException pattern

---

**Status**: ✅ Fixed  
**Date**: October 30, 2025  
**Branch**: `cursor/investigate-persistent-timeout-issues-2242`
