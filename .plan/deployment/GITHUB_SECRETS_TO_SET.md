# GitHub Secrets Configuration

**⚠️ URGENT: These secrets must be configured to fix the deployment error**

## How to Add Secrets

1. Go to: https://github.com/YOUR_USERNAME/navarch-studio/settings/secrets/actions
2. Click **"New repository secret"**
3. Copy the **Name** and **Value** from below
4. Click **"Add secret"**
5. Repeat for each secret

---

## Required Secrets

### ECR Repository URLs (Docker Images)

| Secret Name                | Value                                                                          |
| -------------------------- | ------------------------------------------------------------------------------ |
| `ECR_IDENTITY_SERVICE_URL` | `344870914438.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-identity-service` |
| `ECR_API_GATEWAY_URL`      | `344870914438.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-api-gateway`      |
| `ECR_DATA_SERVICE_URL`     | `344870914438.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-data-service`     |
| `ECR_FRONTEND_URL`         | `344870914438.dkr.ecr.us-east-1.amazonaws.com/navarch-studio-frontend`         |

### AWS Credentials

| Secret Name             | Value                              |
| ----------------------- | ---------------------------------- |
| `AWS_ACCESS_KEY_ID`     | _(Your AWS access key - from IAM)_ |
| `AWS_SECRET_ACCESS_KEY` | _(Your AWS secret key - from IAM)_ |

### Project Configuration

| Secret Name    | Value                                     |
| -------------- | ----------------------------------------- |
| `PROJECT_NAME` | `navarch-studio`                          |
| `COST_CENTER`  | `engineering` _(or your preferred value)_ |
| `DOMAIN_NAME`  | _(leave empty or set custom domain)_      |

### Infrastructure IDs

| Secret Name                    | Value                                                     |
| ------------------------------ | --------------------------------------------------------- |
| `S3_BUCKET_NAME`               | `navarch-studio-terraform-state-344870914438`             |
| `DYNAMODB_TABLE_NAME`          | `navarch-studio-terraform-locks`                          |
| `VPC_ID`                       | `vpc-0470951bae907c718`                                   |
| `APP_RUNNER_SECURITY_GROUP_ID` | `sg-00e1be3e7c3c688bf`                                    |
| `RDS_SECURITY_GROUP_ID`        | `sg-06d53c1ec804275fb`                                    |
| `PUBLIC_SUBNET_IDS`            | `["subnet-04d93f0e14b9f80e0","subnet-0b2002d687ffa37de"]` |

**⚠️ IMPORTANT:** For `PUBLIC_SUBNET_IDS`, copy the entire JSON array including the brackets and quotes.

### Cognito Configuration

| Secret Name                   | Value                        |
| ----------------------------- | ---------------------------- |
| `COGNITO_USER_POOL_ID`        | `us-east-1_z5lKXcFsl`        |
| `COGNITO_USER_POOL_CLIENT_ID` | `2kgtc0rkmi4apirt3bo8paemja` |
| `COGNITO_DOMAIN`              | `navarch-studio-wvf43tbr`    |

### Database Configuration

| Secret Name    | Value                                         |
| -------------- | --------------------------------------------- |
| `RDS_DATABASE` | `navarch_studio_db` _(or your database name)_ |
| `RDS_USERNAME` | `postgres` _(or your database username)_      |
| `RDS_PASSWORD` | _(Your secure database password)_             |

---

## Verification Checklist

After adding all secrets:

- [ ] Navigate to: https://github.com/YOUR_USERNAME/navarch-studio/settings/secrets/actions
- [ ] Verify you see **at least 20 secrets** listed
- [ ] Check that no secret names have typos (they are case-sensitive!)
- [ ] Ensure `PUBLIC_SUBNET_IDS` includes the full JSON array

---

## Next Steps

Once all secrets are configured:

1. **Trigger a new deployment:**

   ```powershell
   git add .
   git commit -m "fix: Update configurations"
   git push origin main
   ```

2. **Monitor the workflow:**
   - Go to: https://github.com/YOUR_USERNAME/navarch-studio/actions
   - Watch the "Deploy to Dev" workflow
   - The Docker build should now succeed!

---

## Alternative: Use GitHub CLI (Faster)

If you install GitHub CLI (https://cli.github.com/):

```powershell
# Authenticate
gh auth login

# Run the automated script
.\scripts\setup-github-secrets.ps1
```

This script will automatically set all secrets from your Terraform state!

---

**Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Terraform State:** terraform/setup/terraform.tfstate
