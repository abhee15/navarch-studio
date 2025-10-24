# IAM Policy Setup for Terraform Template

This file (`iam-policy.tf`) contains Terraform configuration to create an IAM policy with proper permissions for deploying the template infrastructure.

**Location:** `terraform/iam-policy.tf` (standalone, separate from setup/)

## ⚠️ **IMPORTANT: AWS Policy Size Limitation**

**The IAM policy defined in this file exceeds AWS's 6KB limit for managed policies.**

When you run `terraform apply`, you'll get this error:
```
Error: creating IAM Policy: LimitExceeded: Cannot exceed quota for PolicySize: 6144
```

### Practical Workaround (What Actually Works)

Instead of using the full Terraform-managed policy, use this approach:

```powershell
# Option 1: AWS Managed Policies (Quick)
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/PowerUserAccess

# Option 2: Targeted Policies (Better)
# Cognito permissions
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonCognitoPowerUser

# Cost Management permissions (inline policy)
aws iam put-user-policy --user-name YOUR_USERNAME --policy-name CostManagement --policy-document '{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": ["budgets:*", "ce:*"],
    "Resource": "*"
  }]
}'

# EC2/VPC permissions
aws iam attach-user-policy --user-name YOUR_USERNAME --policy-arn arn:aws:iam::aws:policy/AmazonEC2FullAccess

# And so on for other services...
```

**Note:** The `iam-policy.tf` file is still useful as a **reference** for understanding required permissions, but it cannot be directly applied due to AWS size limits.

## 📋 Why Terraform for IAM Policy?

**Benefits over static JSON:**

- ✅ **Single source of truth** - No duplicate files to maintain
- ✅ **Comments in code** - Better documentation
- ✅ **Modular** - Enable/disable resource groups easily
- ✅ **Testable** - Validate with `terraform validate`
- ✅ **Version controlled** - Track changes over time
- ✅ **Generate JSON** - Export to JSON when needed

## 📋 Policy Structure

The IAM policy is organized into logical resource groups:

### 1. **Core Identity** (`core_identity`)

- STS operations to get AWS account ID

### 2. **Terraform State Management** (`terraform_state_*`)

- **S3**: State storage bucket operations
- **DynamoDB**: State locking table operations

### 3. **Networking** (`networking`)

- VPC, Subnets, Internet Gateway
- Route Tables, Security Groups
- All EC2 networking resources

### 4. **Container Registry** (`container_registry`)

- ECR repositories for Docker images
- Image push/pull operations

### 5. **Authentication** (`authentication`)

- Cognito User Pool management
- User pool clients and domains

### 6. **Logging** (`logging`)

- CloudWatch Log Groups
- Log retention policies

### 7. **Cost Management** (`cost_management`)

- AWS Budgets
- Cost anomaly detection

### 8. **Database** (`database`)

- RDS PostgreSQL instances
- Secrets Manager for credentials

### 9. **Compute** (`compute`)

- App Runner services
- VPC connectors

### 10. **IAM Roles** (`iam_roles`)

- Service roles for App Runner
- Limited to app-runner-\* naming

### 11. **CDN** (`cdn`)

- CloudFront distributions
- Origin access control

### 12. **Frontend** (`frontend`)

- S3 buckets for static hosting
- Frontend deployment operations

---

## 🚀 Quick Start

### Option 1: Create Policy Only (Requires Admin Access)

```powershell
# Navigate to terraform directory
cd terraform

# Initialize Terraform
terraform init

# Create ONLY the IAM policy
terraform apply -target=aws_iam_policy.terraform_template

# Get the policy ARN
terraform output policy_arn
```

**Note:** This requires admin/privileged AWS credentials to create IAM policies.

### Option 2: Create Policy + Attach to User

```powershell
# In terraform/ directory
cd terraform

# Create policy with admin credentials
terraform apply -target=aws_iam_policy.terraform_template

# Attach to existing user
$POLICY_ARN = terraform output -raw policy_arn
aws iam attach-user-policy `
  --user-name YOUR_USERNAME `
  --policy-arn $POLICY_ARN

# Now the user can run setup with their own credentials
cd setup
terraform init
terraform apply -var-file=terraform.tfvars
```

### Option 3: Export Policy to JSON (No Terraform State)

```powershell
# In terraform/ directory
cd terraform
terraform init

# Export policy JSON without creating anything
terraform output -raw policy_json | Out-File -FilePath policy-generated.json -Encoding UTF8

# Pretty print
Get-Content policy-generated.json | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Use directly with AWS CLI (no Terraform state created)
aws iam create-policy `
  --policy-name TerraformTemplatePolicy `
  --policy-document file://policy-generated.json
```

---

## 🔧 Customization

### Enable/Disable Resource Groups

Edit `iam-policy.tf` and comment out groups you don't need:

```hcl
data "aws_iam_policy_document" "combined" {
  source_policy_documents = [
    data.aws_iam_policy_document.core_identity.json,
    data.aws_iam_policy_document.terraform_state_s3.json,
    # data.aws_iam_policy_document.cdn.json,  # Disable CloudFront
    # data.aws_iam_policy_document.frontend.json,  # Disable frontend
  ]
}
```

### Add Custom Resource Groups

Create a new policy document:

```hcl
data "aws_iam_policy_document" "my_custom_group" {
  statement {
    sid    = "MyCustomGroup"
    effect = "Allow"
    actions = [
      "service:Action"
    ]
    resources = ["*"]
  }
}

# Add to combined policy
data "aws_iam_policy_document" "combined" {
  source_policy_documents = [
    # ... existing groups
    data.aws_iam_policy_document.my_custom_group.json,
  ]
}
```

---

## 📊 Policy Summary

| Resource Group     | Services        | Actions | Resources             |
| ------------------ | --------------- | ------- | --------------------- |
| Core Identity      | STS             | 1       | \*                    |
| Terraform State    | S3, DynamoDB    | 17      | State buckets/tables  |
| Networking         | EC2             | 30+     | VPC, Subnets, SGs     |
| Container Registry | ECR             | 18      | Repositories          |
| Authentication     | Cognito         | 16      | User pools            |
| Logging            | CloudWatch Logs | 7       | Log groups            |
| Cost Management    | Budgets, CE     | 13      | Budgets, monitors     |
| Database           | RDS, Secrets    | 24      | DB instances, secrets |
| Compute            | App Runner      | 11      | Services, connectors  |
| IAM Roles          | IAM             | 13      | App Runner roles      |
| CDN                | CloudFront      | 10      | Distributions         |
| Frontend           | S3              | 11      | Frontend buckets      |

---

## 🔒 Security Features

### Least Privilege

- Each resource group has minimal required permissions
- Resource ARNs restricted where possible
- No wildcards in sensitive operations

### Resource Restrictions

- S3 buckets: `*-terraform-state-*` and `*-frontend`
- DynamoDB tables: `*-terraform-locks`
- IAM roles: `*-app-runner-*` only
- CloudWatch logs: `/aws/apprunner/*` prefix

### Audit Trail

- All resources tagged with `ManagedBy: Terraform`
- SID values make CloudTrail logs readable
- Policy organized for easy review

---

## 🧪 Testing

### Test Policy Permissions

```powershell
# Assume you have attached the policy
# Test each resource group:

# 1. Core Identity
aws sts get-caller-identity

# 2. Networking
aws ec2 describe-availability-zones

# 3. ECR
aws ecr describe-repositories

# 4. Cognito
aws cognito-idp list-user-pools --max-results 1

# 5. S3
aws s3 ls

# 6. RDS
aws rds describe-db-instances
```

### Validate Policy Syntax

```powershell
# Validate Terraform
terraform validate

# Check policy size (must be < 6144 chars)
terraform output -raw policy_json | Measure-Object -Character

# Pretty print policy
terraform output -raw policy_json | jq .
```

---

## 📝 Maintenance

### Update Policy

1. Edit `iam-policy.tf`
2. Run `terraform plan` to review changes
3. Run `terraform apply` to update
4. Policy updates are applied immediately to attached users

### Version Control

```powershell
# Tag policy versions
git tag -a v1.0.0 -m "Initial IAM policy"

# Rollback if needed
git checkout v1.0.0 -- iam-policy.tf
terraform apply
```

---

## 🆘 Troubleshooting

### Policy Too Large

```
Error: Policy document is too large
```

**Solution:** Split into multiple policies or use AWS managed policies for some groups.

### Statement ID Conflict

```
Error: Duplicate Sid in policy
```

**Solution:** Each statement needs a unique SID. Check `sid` fields in policy documents.

### Resource ARN Issues

```
Error: Invalid resource ARN
```

**Solution:** Verify ARN format matches AWS service requirements. Use `*` for services that don't support resource restrictions.

---

## 📚 Related Files

- `../setup/iam-policy.json` - JSON version (for AWS Console)
- `../../.plan/IAM_SETUP.md` - Complete setup guide
- `../../.cursor/rules` - Project-specific rules

---

## 🎯 Benefits of This Approach

✅ **Modular**: Enable/disable resource groups as needed
✅ **Maintainable**: Each group in separate policy document
✅ **Documented**: Comments explain each permission
✅ **Testable**: Can validate each group independently
✅ **Secure**: Follows least privilege principle
✅ **Auditable**: Clear SIDs in CloudTrail logs

---

_This IAM policy is designed specifically for the Sri Full-Stack Template._
_Last updated: October 2025_





