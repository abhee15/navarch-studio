# Troubleshooting Flowchart

This visual guide helps you decide whether an issue is infrastructure, configuration, or application-related.

## Main Decision Tree

```mermaid
graph TD
    Start[Issue Detected] --> Q1{All commands/queries<br/>return empty?}
    
    Q1 -->|Yes| Infra1[INFRASTRUCTURE ISSUE]
    Q1 -->|No| Q2{Works locally<br/>in docker-compose?}
    
    Q2 -->|Yes| Config1[CONFIGURATION ISSUE]
    Q2 -->|No| Q3{Similar operations<br/>work?}
    
    Q3 -->|Yes| App1[APPLICATION ISSUE]
    Q3 -->|No| Infra2[INFRASTRUCTURE ISSUE]
    
    Infra1 --> InfraCheck[Check Terraform/IaC]
    Infra2 --> InfraCheck
    
    InfraCheck --> InfraQ1{Feature enabled<br/>in Terraform?}
    InfraQ1 -->|No| InfraFix1[Add to Terraform<br/>and deploy]
    InfraQ1 -->|Yes| InfraQ2{Resource exists<br/>in AWS?}
    
    InfraQ2 -->|No| InfraFix2[Deploy infrastructure<br/>or fix Terraform]
    InfraQ2 -->|Yes| InfraQ3{Networking correct?<br/>VPC/SG/Routes}
    
    InfraQ3 -->|No| InfraFix3[Fix security groups<br/>or VPC config]
    InfraQ3 -->|Yes| ConfigCheck[Move to<br/>Configuration Layer]
    
    Config1 --> ConfigCheck
    ConfigCheck --> ConfigQ1{Env vars set?}
    
    ConfigQ1 -->|No| ConfigFix1[Set env vars<br/>in Terraform]
    ConfigQ1 -->|Yes| ConfigQ2{Secrets accessible?}
    
    ConfigQ2 -->|No| ConfigFix2[Fix IAM permissions<br/>or secrets]
    ConfigQ2 -->|Yes| ConfigQ3{Connection strings<br/>correct?}
    
    ConfigQ3 -->|No| ConfigFix3[Fix connection<br/>string format]
    ConfigQ3 -->|Yes| AppCheck[Move to<br/>Application Layer]
    
    App1 --> AppCheck
    AppCheck --> AppQ1{Logs show<br/>exceptions?}
    
    AppQ1 -->|Yes| AppFix1[Fix application<br/>code bug]
    AppQ1 -->|No| AppQ2{Logic error<br/>in code?}
    
    AppQ2 -->|Yes| AppFix2[Debug and fix<br/>business logic]
    AppQ2 -->|No| AppQ3{Input validation<br/>issue?}
    
    AppQ3 -->|Yes| AppFix3[Add validation<br/>or error handling]
    AppQ3 -->|No| Escalate[Escalate:<br/>Check AWS Health<br/>or external deps]
    
    InfraFix1 --> Verify[Verify Fix]
    InfraFix2 --> Verify
    InfraFix3 --> Verify
    ConfigFix1 --> Verify
    ConfigFix2 --> Verify
    ConfigFix3 --> Verify
    AppFix1 --> Verify
    AppFix2 --> Verify
    AppFix3 --> Verify
    
    Verify --> Success{Issue<br/>resolved?}
    Success -->|Yes| Done[✓ Done]
    Success -->|No| NextLayer[Check next layer<br/>or re-evaluate]
    
    style Infra1 fill:#ff6b6b
    style Infra2 fill:#ff6b6b
    style Config1 fill:#ffd93d
    style App1 fill:#6bcf7f
    style Done fill:#95e1d3
```

## Quick Reference Guide

### 1. Infrastructure Layer Issues (🔴 Red)

**Symptoms:**
- All queries/commands return empty
- Resource not found errors
- No logs/metrics anywhere
- Connection timeout immediately
- Works locally but not in cloud

**Where to Look:**
- `terraform/deploy/modules/*/main.tf`
- `terraform/setup/networking.tf`
- AWS Console resource status

**Common Fixes:**
- Add missing Terraform resource
- Enable feature in Terraform (e.g., observability)
- Fix security group rules
- Configure VPC connector
- Deploy infrastructure

### 2. Configuration Layer Issues (🟡 Yellow)

**Symptoms:**
- "Configuration not found" errors
- "Access denied" errors
- Inconsistent behavior across environments
- Works with hardcoded values
- Wrong service endpoints

**Where to Look:**
- `terraform/deploy/modules/app-runner/main.tf` (env vars section)
- AWS Secrets Manager
- IAM roles and policies
- Environment-specific tfvars

**Common Fixes:**
- Set missing environment variables
- Fix secret format (JSON vs plain string)
- Update IAM permissions
- Correct connection strings
- Fix service URLs

### 3. Application Layer Issues (🟢 Green)

**Symptoms:**
- Specific operations fail
- Business logic errors
- Data validation failures
- Exceptions in logs
- Incorrect calculations

**Where to Look:**
- Application logs in CloudWatch
- Controller/Service code
- Database queries
- Business logic

**Common Fixes:**
- Fix code bugs
- Add error handling
- Improve validation
- Fix business logic
- Update database queries

## Scenario-Based Flowcharts

### Scenario: No Logs Available

```mermaid
graph LR
    A[No logs available] --> B{All services<br/>have no logs?}
    B -->|Yes| C[Infra: observability<br/>not configured]
    B -->|No| D{Specific service<br/>has no logs?}
    D -->|Yes| E{Service<br/>starting?}
    E -->|No| F[App: Check why<br/>service crashes]
    E -->|Yes| G[Config: Check log<br/>level settings]
    
    style C fill:#ff6b6b
    style F fill:#6bcf7f
    style G fill:#ffd93d
```

### Scenario: Database Connection Fails

```mermaid
graph LR
    A[DB connection fails] --> B{Timeout<br/>immediately?}
    B -->|Yes| C[Infra: Security<br/>groups or VPC]
    B -->|No| D{Authentication<br/>error?}
    D -->|Yes| E[Config: Wrong<br/>credentials]
    D -->|No| F{Specific query<br/>fails?}
    F -->|Yes| G[App: SQL syntax<br/>or logic error]
    F -->|No| H[Infra: RDS not<br/>running]
    
    style C fill:#ff6b6b
    style E fill:#ffd93d
    style G fill:#6bcf7f
    style H fill:#ff6b6b
```

### Scenario: API Returns 500 Error

```mermaid
graph LR
    A[API 500 error] --> B{Logs show<br/>exception?}
    B -->|No logs| C[Infra: Logging<br/>not configured]
    B -->|Connection error| D[Infra: Downstream<br/>service unreachable]
    B -->|Null reference| E[App: Code bug]
    B -->|Config missing| F[Config: Env var<br/>not set]
    
    style C fill:#ff6b6b
    style D fill:#ff6b6b
    style E fill:#6bcf7f
    style F fill:#ffd93d
```

### Scenario: CORS Error in Browser

```mermaid
graph LR
    A[CORS error] --> B{Origin in<br/>allowed list?}
    B -->|Not in list| C[Config: Add origin<br/>to env vars]
    B -->|In list| D{Protocol<br/>matches?}
    D -->|No| E[Config: Add https://<br/>to origin]
    D -->|Yes| F{API responds<br/>to OPTIONS?}
    F -->|No| G[App: CORS middleware<br/>not configured]
    F -->|Yes| H[App: Headers<br/>not set correctly]
    
    style C fill:#ffd93d
    style E fill:#ffd93d
    style G fill:#6bcf7f
    style H fill:#6bcf7f
```

## Red Flags Checklist

Before debugging, check for these red flags:

### 🚩 Infrastructure Red Flags
- [ ] ALL queries return empty (not just one)
- [ ] "Resource not found" from AWS
- [ ] No logs/metrics/traces anywhere
- [ ] Connection timeout < 5 seconds
- [ ] Works locally, fails in all cloud environments

**If any checked → Infrastructure issue**

### 🚩 Configuration Red Flags
- [ ] Works in one environment, fails in another
- [ ] "Access denied" or "Unauthorized"
- [ ] Works with hardcoded values
- [ ] Inconsistent behavior
- [ ] Wrong service endpoint errors

**If any checked → Configuration issue**

### 🚩 Application Red Flags
- [ ] Specific operation fails consistently
- [ ] Exception in logs
- [ ] Data validation error
- [ ] Business logic returns wrong result
- [ ] Works for some inputs, fails for others

**If any checked → Application issue**

## Debugging Time Estimates

To help prioritize and avoid over-investing time:

| Issue Type | Typical Resolution Time | Max Before Escalating |
|------------|-------------------------|----------------------|
| Infrastructure - Missing resource | 15-30 min (Terraform + deploy) | 1 hour |
| Infrastructure - Networking | 30-60 min (diagnose + fix) | 2 hours |
| Configuration - Env vars | 10-20 min (update + deploy) | 30 minutes |
| Configuration - Secrets | 20-40 min (fix format + permissions) | 1 hour |
| Application - Simple bug | 15-30 min (fix + test) | 1 hour |
| Application - Complex logic | 1-3 hours (debug + test + verify) | 4 hours |

**Rule:** If you exceed max time, stop and re-evaluate from Layer 1.

## Summary: The 3-Question Method

When debugging any issue, ask these 3 questions in order:

1. **Is the infrastructure deployed and configured correctly?**
   - Read Terraform
   - Verify in AWS Console
   - Check networking

2. **Is the configuration set correctly?**
   - Check env vars
   - Verify secrets
   - Validate connection strings

3. **Is the application code correct?**
   - Review logs
   - Debug code
   - Test logic

**Stop at the first "No" and fix that layer before proceeding.**

## Further Reading

- [Full Debugging Methodology](./debugging-methodology.md)
- [Terraform Debugging Guide](./terraform.md#debugging-terraform-managed-infrastructure)
- [.NET Cloud Debugging](./dotnet.md#debugging-net-applications-in-cloud)
- [React Debugging Guide](./react-typescript.md#debugging-frontend-issues)

