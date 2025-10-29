# Debugging Methodology

## Core Principle

**Always validate infrastructure exists before debugging configuration or application logic.**

When troubleshooting issues, work from the foundation up:

1. **Infrastructure Layer** (Terraform/IaC) - Check FIRST
2. **Configuration Layer** (env vars, secrets) - Check SECOND
3. **Application Layer** (code logic) - Check LAST

## The Problem This Solves

Common anti-pattern:

```
Issue: "Logs are empty"
❌ Wrong: Try 10 different log query variations
✅ Right: Check if logging is configured in Terraform first
```

This methodology prevents:

- Wasting time debugging symptoms
- Chasing red herrings
- Attempting to fix what isn't broken
- Missing foundational issues

## Three-Layer Hierarchy

### Layer 1: Infrastructure (Check FIRST)

**What to validate:**

- Does the AWS resource exist?
- Is the required feature enabled in IaC?
- Are networking components configured (VPC, subnets, security groups)?
- Is the infrastructure in the expected state?

**How to validate:**

```bash
# Read the Terraform configuration
cat terraform/deploy/modules/<service>/main.tf

# Verify resources exist
aws <service> describe-* --region <region>

# Check resource state in AWS Console
```

**Example - CloudWatch Logs Not Available:**

```
Step 1: Read terraform/deploy/modules/app-runner/main.tf
Step 2: Search for "observability_configuration"
Step 3: If missing → Add to Terraform (infrastructure fix)
Step 4: If present → Move to Layer 2
```

### Layer 2: Configuration (Check SECOND)

**What to validate:**

- Are environment variables set correctly?
- Are secrets accessible?
- Are connection strings formatted properly?
- Are IAM permissions configured?

**How to validate:**

```bash
# Check environment variables
aws apprunner describe-service --service-arn <arn> | grep -A 20 "RuntimeEnvironmentVariables"

# Check secrets
aws secretsmanager get-secret-value --secret-id <id>

# Verify IAM permissions
aws iam get-role-policy --role-name <role> --policy-name <policy>
```

**Example - Database Connection Failing:**

```
Step 1: Verify RDS exists and is available (Layer 1) ✓
Step 2: Check connection string format
Step 3: Verify security groups allow traffic
Step 4: Check database credentials in Secrets Manager
Step 5: If all correct → Move to Layer 3
```

### Layer 3: Application (Check LAST)

**What to validate:**

- Is the application code correct?
- Are there logic errors?
- Are there runtime exceptions?

**How to validate:**

```bash
# Check application logs
aws logs tail /aws/apprunner/<service>/service --since 10m

# Review code logic
# Add additional logging
# Check for exceptions
```

**Example - API Endpoint Returns 500:**

```
Step 1: Verify infrastructure exists (Layer 1) ✓
Step 2: Verify configuration is correct (Layer 2) ✓
Step 3: Check application logs for exceptions
Step 4: Review controller/service code
Step 5: Debug business logic
```

## Red Flags Requiring Infrastructure Review

**Stop and check infrastructure if you see:**

1. **ALL queries/commands return empty** (not just one)

   - Example: All log queries return nothing
   - Likely cause: Logging not configured in infrastructure

2. **Commands work locally but fail in cloud**

   - Example: App works in docker-compose, fails in App Runner
   - Likely cause: Missing cloud infrastructure configuration

3. **"Resource not found" errors**

   - Example: `ResourceNotFoundException` from AWS
   - Likely cause: Resource not deployed or wrong identifier

4. **No logs/metrics/traces anywhere**

   - Example: No CloudWatch logs, no X-Ray traces
   - Likely cause: Observability not enabled in Terraform

5. **Connection timeouts on first request**

   - Example: Database connection times out immediately
   - Likely cause: Security groups, VPC configuration, or network routing

6. **Repeated pattern of failures across services**
   - Example: All microservices fail similarly
   - Likely cause: Shared infrastructure issue (VPC, IAM, etc.)

## Cloud-Specific Debugging Rules

### AWS App Runner Observability

**Before debugging App Runner issues:**

1. ✅ Check if `observability_configuration` block exists in Terraform
2. ✅ Verify CloudWatch Log groups exist:
   ```bash
   aws logs describe-log-groups --region us-east-1 --query "logGroups[?contains(logGroupName, 'apprunner')].logGroupName"
   ```
3. ✅ Check if service has observability enabled:
   ```bash
   aws apprunner describe-service --service-arn <arn> | grep -A 5 "ObservabilityConfiguration"
   ```
4. Only after confirming infrastructure → Query logs

**Rule:** If ALL App Runner services have empty logs, the observability configuration is missing.

### AWS RDS Database Issues

**Before debugging database connection:**

1. ✅ Verify RDS instance exists and is "available":
   ```bash
   aws rds describe-db-instances --region us-east-1 --query "DBInstances[?contains(DBInstanceIdentifier, '<env>')].{ID:DBInstanceIdentifier,Status:DBInstanceStatus,Endpoint:Endpoint.Address}"
   ```
2. ✅ Check security groups allow traffic from App Runner:
   ```bash
   aws ec2 describe-security-groups --group-ids <sg-id>
   ```
3. ✅ Verify VPC connector is attached to App Runner services
4. ✅ Check if RDS is in correct subnets
5. ✅ Verify connection string format: `Host=<endpoint>;Port=5432;Database=<db>;Username=<user>;Password=<pass>`
6. Only after confirming infrastructure → Debug application queries

**Rule:** If connection times out immediately, it's a networking/security group issue, not application code.

### AWS Lambda Issues

**Before debugging Lambda functions:**

1. ✅ Check if CloudWatch Logs permissions exist in IAM role
2. ✅ Verify Lambda has VPC configuration (if accessing RDS)
3. ✅ Check if Lambda timeout is sufficient
4. ✅ Verify environment variables are set
5. Only after confirming infrastructure → Debug function code

### ECS/Fargate Issues

**Before debugging ECS tasks:**

1. ✅ Check if task definition has `logConfiguration` block
2. ✅ Verify CloudWatch Log group exists
3. ✅ Check IAM execution role has `logs:CreateLogStream` and `logs:PutLogEvents`
4. ✅ Verify task networking configuration
5. Only after confirming infrastructure → Debug container code

## Decision Tree

Use this flowchart when debugging: [See troubleshooting-flowchart.md]

**Quick Decision Guide:**

```
Is the issue affecting ALL services/resources?
├─ YES → Infrastructure issue (Layer 1)
└─ NO ┐
      │
      Does it work locally?
      ├─ YES → Configuration issue (Layer 2)
      └─ NO ┐
            │
            Do other similar operations work?
            ├─ YES → Application issue (Layer 3)
            └─ NO → Infrastructure issue (Layer 1)
```

## Common Scenarios

### Scenario 1: No CloudWatch Logs Available

❌ **Wrong Approach:**

```
1. Try aws logs tail with different time ranges
2. Try different log group names
3. Try different query formats
4. Try AWS Console
5. Give up and guess what's wrong
```

✅ **Correct Approach:**

```
1. Notice: ALL log queries return empty
2. Red flag: This indicates infrastructure issue
3. Read terraform/deploy/modules/app-runner/main.tf
4. Search for: observability_configuration
5. Finding: Block is missing
6. Fix: Add observability_configuration to Terraform
7. Deploy: Apply Terraform changes
8. Verify: Logs now available
```

### Scenario 2: Database Connection Timeout

❌ **Wrong Approach:**

```
1. Debug connection string format
2. Try different database drivers
3. Add retry logic
4. Increase timeout values
5. Rewrite database access code
```

✅ **Correct Approach:**

```
1. Notice: Connection times out immediately (< 1 second)
2. Red flag: Network/security issue, not application
3. Check Layer 1: RDS exists and is available ✓
4. Check Layer 1: Security groups allow traffic? ✗
5. Finding: RDS security group doesn't allow App Runner security group
6. Fix: Update security group rules in Terraform
7. Deploy: Apply Terraform changes
8. Verify: Connection succeeds
```

### Scenario 3: CORS Errors in Browser

❌ **Wrong Approach:**

```
1. Try different CORS middleware configurations
2. Add wildcard CORS (*)
3. Modify application code
4. Debug for hours
```

✅ **Correct Approach:**

```
1. Notice: CORS error for specific origin
2. Check Layer 2: Environment variable `Cors__AllowedOrigins__*`
3. Check Terraform: How is this env var set?
4. Finding: CloudFront URL missing `https://` protocol
5. Fix: Update Terraform to include protocol
6. Deploy: Apply Terraform changes
7. Verify: CORS works
```

## Debugging Checklist

Before attempting to debug an issue, answer these questions:

### Infrastructure Validation

- [ ] Have I read the relevant Terraform configuration?
- [ ] Have I verified the resource exists in AWS?
- [ ] Have I checked if the required feature is enabled?
- [ ] Have I verified networking configuration (VPC, subnets, security groups)?
- [ ] Have I checked IAM permissions?

### Configuration Validation

- [ ] Have I verified all environment variables are set?
- [ ] Have I checked secrets are accessible?
- [ ] Have I validated connection strings?
- [ ] Have I checked service-to-service connectivity?

### Application Validation

- [ ] Have I checked application logs?
- [ ] Have I reviewed recent code changes?
- [ ] Have I tested locally?
- [ ] Have I checked for exceptions?

**If you can't check "yes" for all items in a layer, STOP and fix that layer first.**

## Anti-Patterns to Avoid

### 1. Symptom Chasing

❌ Trying 10 variations of the same command that can't possibly work
✅ Check if infrastructure supports the command first

### 2. Assuming Infrastructure

❌ "The infrastructure must be fine, it's probably code"
✅ Always verify infrastructure explicitly

### 3. Skipping Layers

❌ Jumping straight to debugging application code
✅ Work through layers systematically

### 4. Not Reading IaC

❌ Guessing what infrastructure is deployed
✅ Read Terraform files to understand actual state

### 5. Local-First Debugging

❌ Assuming cloud works like local environment
✅ Verify cloud-specific configurations (observability, networking, IAM)

## Integration with CI/CD

When debugging CI/CD failures:

1. **Build Failures** → Application Layer (code issues)
2. **Deployment Failures** → Configuration Layer (env vars, secrets)
3. **Service Health Check Failures** → Infrastructure Layer (networking, resources)
4. **All Services Failing** → Infrastructure Layer (shared infrastructure)

## When to Escalate

If after checking all three layers you still can't resolve:

1. Document what you've checked at each layer
2. Capture exact error messages and logs
3. Note any recent infrastructure changes
4. Review AWS service health dashboard
5. Consider if it's an AWS service issue

## Summary

**Golden Rule:** Infrastructure → Configuration → Application (always top-down)

**Key Insight:** If the foundation is broken, fixing the application won't help.

**Remember:** Time spent validating infrastructure is never wasted. Time spent debugging application code when infrastructure is broken is always wasted.

**Cross-References:**

- [Troubleshooting Flowchart](./troubleshooting-flowchart.md)
- [Terraform Debugging](./terraform.md#debugging-terraform-managed-infrastructure)
- [.NET Cloud Debugging](./dotnet.md#debugging-net-applications-in-cloud)
- [React Debugging](./react-typescript.md#debugging-frontend-issues)
