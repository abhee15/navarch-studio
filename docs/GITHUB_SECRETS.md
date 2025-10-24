# GitHub Secrets Configuration

This document lists all required GitHub secrets for CI/CD workflows. These secrets are used to deploy and manage your infrastructure and applications across different environments.

## 📋 Required Secrets

### AWS Credentials

| Secret Name             | Description             | Example                                    | How to Get                |
| ----------------------- | ----------------------- | ------------------------------------------ | ------------------------- |
| `AWS_ACCESS_KEY_ID`     | AWS IAM user access key | `AKIAIOSFODNN7EXAMPLE`                     | From IAM user credentials |
| `AWS_SECRET_ACCESS_KEY` | AWS IAM user secret key | `wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY` | From IAM user credentials |

### Project Configuration

| Secret Name    | Description              | Example        | How to Get                 |
| -------------- | ------------------------ | -------------- | -------------------------- |
| `PROJECT_NAME` | Project name             | `sri-template` | Your chosen project name   |
| `COST_CENTER`  | Cost center tag          | `engineering`  | Your department/team name  |
| `DOMAIN_NAME`  | Custom domain (optional) | `example.com`  | Your domain or leave empty |

### Infrastructure IDs (from Phase 4)

| Secret Name                    | Description                    | Example                                     | How to Get                                                    |
| ------------------------------ | ------------------------------ | ------------------------------------------- | ------------------------------------------------------------- |
| `S3_BUCKET_NAME`               | Terraform state bucket         | `sri-template-terraform-state-123456789012` | `terraform output -raw s3_bucket_name` (from terraform/setup) |
| `DYNAMODB_TABLE_NAME`          | Terraform lock table           | `sri-template-terraform-locks`              | `terraform output -raw dynamodb_table_name`                   |
| `VPC_ID`                       | VPC ID                         | `vpc-0123456789abcdef0`                     | `terraform output -raw vpc_id`                                |
| `PUBLIC_SUBNET_IDS`            | Public subnet IDs (JSON array) | `["subnet-abc123","subnet-def456"]`         | `terraform output -json public_subnet_ids`                    |
| `APP_RUNNER_SECURITY_GROUP_ID` | App Runner SG                  | `sg-0123456789abcdef0`                      | `terraform output -raw app_runner_security_group_id`          |
| `RDS_SECURITY_GROUP_ID`        | RDS security group             | `sg-0123456789abcdef1`                      | `terraform output -raw rds_security_group_id`                 |

### ECR Repository URLs

| Secret Name                | Description          | Example                                                                      | How to Get                                                                |
| -------------------------- | -------------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `ECR_IDENTITY_SERVICE_URL` | Identity Service ECR | `123456789012.dkr.ecr.us-east-1.amazonaws.com/sri-template-identity-service` | `terraform output -json ecr_repository_urls \| jq -r '.identity_service'` |
| `ECR_API_GATEWAY_URL`      | API Gateway ECR      | `123456789012.dkr.ecr.us-east-1.amazonaws.com/sri-template-api-gateway`      | `terraform output -json ecr_repository_urls \| jq -r '.api_gateway'`      |
| `ECR_DATA_SERVICE_URL`     | Data Service ECR     | `123456789012.dkr.ecr.us-east-1.amazonaws.com/sri-template-data-service`     | `terraform output -json ecr_repository_urls \| jq -r '.data_service'`     |
| `ECR_FRONTEND_URL`         | Frontend ECR         | `123456789012.dkr.ecr.us-east-1.amazonaws.com/sri-template-frontend`         | `terraform output -json ecr_repository_urls \| jq -r '.frontend'`         |

### Cognito Configuration

| Secret Name                   | Description          | Example                      | How to Get                                          |
| ----------------------------- | -------------------- | ---------------------------- | --------------------------------------------------- |
| `COGNITO_USER_POOL_ID`        | Cognito User Pool ID | `us-east-1_A1B2C3D4E`        | `terraform output -raw cognito_user_pool_id`        |
| `COGNITO_USER_POOL_CLIENT_ID` | Cognito Client ID    | `1a2b3c4d5e6f7g8h9i0j1k2l3m` | `terraform output -raw cognito_user_pool_client_id` |

### Database Configuration

| Secret Name    | Description   | Example                  | How to Get                                           |
| -------------- | ------------- | ------------------------ | ---------------------------------------------------- |
| `RDS_DATABASE` | Database name | `sri_template_db`        | From your terraform variables or `terraform output`  |
| `RDS_USERNAME` | DB username   | `postgres`               | From your terraform variables (default: `postgres`)  |
| `RDS_PASSWORD` | DB password   | `YourSecurePassword123!` | From your terraform variables or AWS Secrets Manager |

### Optional: Code Coverage

| Secret Name     | Description          | Example                                | How to Get                                                        |
| --------------- | -------------------- | -------------------------------------- | ----------------------------------------------------------------- |
| `CODECOV_TOKEN` | Codecov upload token | `a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6` | From [codecov.io](https://codecov.io) after connecting repository |

## 🔧 How to Add Secrets

### Using GitHub Web UI

1. Go to your GitHub repository
2. Click on **Settings** tab
3. Click on **Secrets and variables** → **Actions** in the left sidebar
4. Click on **New repository secret**
5. Enter the secret **Name** (exact case-sensitive match)
6. Enter the secret **Value**
7. Click **Add secret**
8. Repeat for all secrets

### Using GitHub CLI

```bash
# Set AWS credentials
gh secret set AWS_ACCESS_KEY_ID --body "YOUR_ACCESS_KEY"
gh secret set AWS_SECRET_ACCESS_KEY --body "YOUR_SECRET_KEY"

# Set project configuration
gh secret set PROJECT_NAME --body "sri-template"
gh secret set COST_CENTER --body "engineering"
gh secret set DOMAIN_NAME --body ""  # Leave empty if no custom domain

# Fetch and set infrastructure IDs from Terraform (requires `jq` and `terraform`)
cd terraform/setup
gh secret set S3_BUCKET_NAME --body "$(terraform output -raw s3_bucket_name)"
gh secret set DYNAMODB_TABLE_NAME --body "$(terraform output -raw dynamodb_table_name)"
gh secret set VPC_ID --body "$(terraform output -raw vpc_id)"
gh secret set PUBLIC_SUBNET_IDS --body "$(terraform output -json public_subnet_ids)"
gh secret set APP_RUNNER_SECURITY_GROUP_ID --body "$(terraform output -raw app_runner_security_group_id)"
gh secret set RDS_SECURITY_GROUP_ID --body "$(terraform output -raw rds_security_group_id)"

# ECR repository URLs
gh secret set ECR_IDENTITY_SERVICE_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.identity_service')"
gh secret set ECR_API_GATEWAY_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.api_gateway')"
gh secret set ECR_DATA_SERVICE_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.data_service')"
gh secret set ECR_FRONTEND_URL --body "$(terraform output -json ecr_repository_urls | jq -r '.frontend')"

# Cognito
gh secret set COGNITO_USER_POOL_ID --body "$(terraform output -raw cognito_user_pool_id)"
gh secret set COGNITO_USER_POOL_CLIENT_ID --body "$(terraform output -raw cognito_user_pool_client_id)"

# Database (set these manually with your values)
gh secret set RDS_DATABASE --body "sri_template_db"
gh secret set RDS_USERNAME --body "postgres"
gh secret set RDS_PASSWORD --body "YOUR_SECURE_PASSWORD"
```

### Using PowerShell (Windows)

```powershell
# Fetch Terraform outputs
cd terraform/setup

# Set secrets using GitHub CLI
gh secret set S3_BUCKET_NAME --body (terraform output -raw s3_bucket_name)
gh secret set DYNAMODB_TABLE_NAME --body (terraform output -raw dynamodb_table_name)
gh secret set VPC_ID --body (terraform output -raw vpc_id)
gh secret set PUBLIC_SUBNET_IDS --body (terraform output -json public_subnet_ids)
gh secret set APP_RUNNER_SECURITY_GROUP_ID --body (terraform output -raw app_runner_security_group_id)
gh secret set RDS_SECURITY_GROUP_ID --body (terraform output -raw rds_security_group_id)

# ECR URLs
$ecrUrls = terraform output -json ecr_repository_urls | ConvertFrom-Json
gh secret set ECR_IDENTITY_SERVICE_URL --body $ecrUrls.identity_service
gh secret set ECR_API_GATEWAY_URL --body $ecrUrls.api_gateway
gh secret set ECR_DATA_SERVICE_URL --body $ecrUrls.data_service
gh secret set ECR_FRONTEND_URL --body $ecrUrls.frontend

# Cognito
gh secret set COGNITO_USER_POOL_ID --body (terraform output -raw cognito_user_pool_id)
gh secret set COGNITO_USER_POOL_CLIENT_ID --body (terraform output -raw cognito_user_pool_client_id)
```

## 🌍 Environment-Specific Secrets

Some workflows use **GitHub Environments** for environment-specific configuration. Configure these in:

**Settings** → **Environments** → **[dev/staging/production]** → **Add secret**

### Recommended Environment Secrets

For each environment (dev, staging, prod), you may want to override:

- `RDS_PASSWORD` - Different password per environment
- `DOMAIN_NAME` - Different domain per environment (e.g., dev.example.com, staging.example.com, example.com)

## 🔒 Security Best Practices

1. **Never commit secrets to the repository**

   - Use `.gitignore` for local config files
   - GitHub will scan for accidentally committed secrets

2. **Use least privilege AWS credentials**

   - Create a dedicated IAM user for CI/CD
   - Attach only necessary policies (see `.plan/IAM_SETUP.md`)

3. **Rotate secrets regularly**

   - Update AWS access keys every 90 days
   - Rotate database passwords periodically

4. **Use environment protection rules**

   - Require manual approval for production deployments
   - Configure in **Settings** → **Environments** → **production** → **Required reviewers**

5. **Monitor secret usage**

   - Check AWS CloudTrail for API calls
   - Review GitHub Actions logs for anomalies

6. **Use environment-specific secrets when possible**
   - Keep dev/staging/prod credentials separate
   - Limit blast radius of compromised credentials

## 📝 Verification Checklist

After adding all secrets, verify:

- [ ] All secrets listed above are added
- [ ] Secret names match exactly (case-sensitive)
- [ ] No trailing spaces in secret values
- [ ] AWS credentials have proper permissions
- [ ] Test with a manual workflow run
- [ ] Check workflow logs for any missing secrets errors

## 🚨 Troubleshooting

### "Secret not found" error

**Cause:** Secret name mismatch or not set.

**Solution:** Check exact secret name in workflow YAML and GitHub Settings.

### "Access Denied" in AWS operations

**Cause:** Insufficient IAM permissions.

**Solution:** Review IAM policy in `.plan/IAM_SETUP.md` and ensure all permissions are granted.

### "Invalid credentials" for ECR login

**Cause:** AWS credentials expired or incorrect region.

**Solution:**

- Verify `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`
- Ensure `AWS_REGION` matches your infrastructure region

### Terraform state lock errors

**Cause:** Incorrect S3 bucket or DynamoDB table name.

**Solution:**

- Verify `S3_BUCKET_NAME` matches actual bucket
- Verify `DYNAMODB_TABLE_NAME` matches actual table
- Check bucket/table exist in AWS console

## 📚 Additional Resources

- [GitHub Actions Encrypted Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [GitHub CLI Secret Management](https://cli.github.com/manual/gh_secret)
- [AWS IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html)
- [Terraform Backend Configuration](https://www.terraform.io/language/settings/backends/configuration)

## 🔄 Updating Secrets

When infrastructure changes (e.g., new VPC, new ECR repos):

1. Run `terraform apply` in `terraform/setup`
2. Update affected secrets using the commands above
3. Re-run failed workflows if any

---

**Last Updated:** Phase 7 - CI/CD Pipeline Setup





