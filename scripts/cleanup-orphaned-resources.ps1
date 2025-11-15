# Cleanup Orphaned AWS Resources Script (PowerShell)
# This script manually deletes AWS resources that exist but are not tracked in Terraform state

param(
    [string]$Environment = "dev",
    [string]$Project = "navarch-studio",
    [string]$Region = "us-east-1"
)

Write-Host "🧹 Cleaning up orphaned AWS resources for environment: $Environment" -ForegroundColor Cyan
Write-Host "Project: $Project"
Write-Host "Region: $Region"
Write-Host ""

# VPC Connector
Write-Host "🔍 Checking VPC Connector..." -ForegroundColor Yellow
$vpcConnArn = aws apprunner list-vpc-connectors `
    --region $Region `
    --query "VpcConnectors[?VpcConnectorName=='$Project-$Environment-vpc-connector'].VpcConnectorArn | [0]" `
    --output text 2>$null

if ($vpcConnArn -and $vpcConnArn -ne "None") {
    Write-Host "🗑️ Deleting VPC Connector: $vpcConnArn" -ForegroundColor Red
    aws apprunner delete-vpc-connector --vpc-connector-arn $vpcConnArn --region $Region
    Write-Host "✅ VPC Connector deleted" -ForegroundColor Green
} else {
    Write-Host "✓ VPC Connector not found" -ForegroundColor Gray
}

# IAM Roles
Write-Host ""
Write-Host "🔍 Checking IAM Roles..." -ForegroundColor Yellow
foreach ($role in @("app-runner-ecr-role", "app-runner-instance-role")) {
    $roleName = "$Project-$Environment-$role"
    
    $roleExists = aws iam get-role --role-name $roleName 2>$null
    if ($roleExists) {
        Write-Host "🗑️ Detaching policies and deleting IAM role: $roleName" -ForegroundColor Red
        
        # Detach managed policies
        $attachedPolicies = aws iam list-attached-role-policies --role-name $roleName --query 'AttachedPolicies[].PolicyArn' --output text 2>$null
        if ($attachedPolicies) {
            foreach ($policyArn in $attachedPolicies -split '\s+') {
                Write-Host "  - Detaching policy: $policyArn"
                aws iam detach-role-policy --role-name $roleName --policy-arn $policyArn 2>$null
            }
        }
        
        # Delete inline policies
        $inlinePolicies = aws iam list-role-policies --role-name $roleName --query 'PolicyNames[]' --output text 2>$null
        if ($inlinePolicies) {
            foreach ($policyName in $inlinePolicies -split '\s+') {
                Write-Host "  - Deleting inline policy: $policyName"
                aws iam delete-role-policy --role-name $roleName --policy-name $policyName 2>$null
            }
        }
        
        # Delete role
        aws iam delete-role --role-name $roleName
        Write-Host "✅ IAM role deleted: $roleName" -ForegroundColor Green
    } else {
        Write-Host "✓ IAM role not found: $roleName" -ForegroundColor Gray
    }
}

# IAM Policy
Write-Host ""
Write-Host "🔍 Checking IAM Policy..." -ForegroundColor Yellow
$policyName = "$Project-$Environment-secrets-access"
$policyArn = aws iam list-policies `
    --query "Policies[?PolicyName=='$policyName'].Arn | [0]" `
    --output text 2>$null

if ($policyArn -and $policyArn -ne "None") {
    Write-Host "🗑️ Deleting IAM policy: $policyName" -ForegroundColor Red
    aws iam delete-policy --policy-arn $policyArn
    Write-Host "✅ IAM policy deleted" -ForegroundColor Green
} else {
    Write-Host "✓ IAM policy not found" -ForegroundColor Gray
}

# Secrets Manager
Write-Host ""
Write-Host "🔍 Checking Secrets Manager..." -ForegroundColor Yellow
$secretName = "$Project-$Environment-db-password"
$secretExists = aws secretsmanager describe-secret --secret-id $secretName --region $Region 2>$null
if ($secretExists) {
    Write-Host "🗑️ Deleting secret: $secretName" -ForegroundColor Red
    aws secretsmanager delete-secret `
        --secret-id $secretName `
        --force-delete-without-recovery `
        --region $Region
    Write-Host "✅ Secret deleted" -ForegroundColor Green
} else {
    Write-Host "✓ Secret not found" -ForegroundColor Gray
}

# DB Subnet Group
Write-Host ""
Write-Host "🔍 Checking DB Subnet Group..." -ForegroundColor Yellow
$subnetGroup = "$Project-$Environment-db-subnet-group"
$subnetExists = aws rds describe-db-subnet-groups --db-subnet-group-name $subnetGroup --region $Region 2>$null
if ($subnetExists) {
    Write-Host "🗑️ Deleting DB subnet group: $subnetGroup" -ForegroundColor Red
    aws rds delete-db-subnet-group --db-subnet-group-name $subnetGroup --region $Region
    Write-Host "✅ DB subnet group deleted" -ForegroundColor Green
} else {
    Write-Host "✓ DB subnet group not found" -ForegroundColor Gray
}

# S3 Buckets
Write-Host ""
Write-Host "🔍 Checking S3 Buckets..." -ForegroundColor Yellow
foreach ($bucketSuffix in @("frontend", "benchmark-raw", "benchmark-curated")) {
    $bucketName = "$Project-$Environment-$bucketSuffix"
    
    $bucketExists = aws s3api head-bucket --bucket $bucketName --region $Region 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "🗑️ Emptying and deleting bucket: $bucketName" -ForegroundColor Red
        aws s3 rm "s3://$bucketName" --recursive --region $Region 2>$null
        aws s3 rb "s3://$bucketName" --region $Region
        Write-Host "✅ Bucket deleted: $bucketName" -ForegroundColor Green
    } else {
        Write-Host "✓ Bucket not found: $bucketName" -ForegroundColor Gray
    }
}

# CloudFront OAC
Write-Host ""
Write-Host "🔍 Checking CloudFront Origin Access Control..." -ForegroundColor Yellow
$oacId = aws cloudfront list-origin-access-controls `
    --query "OriginAccessControlList.Items[?Name=='$Project-$Environment-oac'].Id | [0]" `
    --output text 2>$null

if ($oacId -and $oacId -ne "None") {
    Write-Host "🗑️ Deleting CloudFront OAC: $oacId" -ForegroundColor Red
    $etag = aws cloudfront get-origin-access-control --id $oacId --query 'ETag' --output text
    aws cloudfront delete-origin-access-control --id $oacId --if-match $etag
    Write-Host "✅ CloudFront OAC deleted" -ForegroundColor Green
} else {
    Write-Host "✓ CloudFront OAC not found" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Cleanup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "You can now run a fresh Terraform apply:" -ForegroundColor Cyan
Write-Host "  cd terraform/deploy" -ForegroundColor White
Write-Host "  terraform init -reconfigure" -ForegroundColor White
Write-Host "  terraform apply -var-file=`"environments/$Environment.tfvars`"" -ForegroundColor White

















