#!/bin/bash

# Cleanup Orphaned AWS Resources Script
# This script manually deletes AWS resources that exist but are not tracked in Terraform state

set -e

ENV="${1:-dev}"
PROJECT="${2:-navarch-studio}"
REGION="${3:-us-east-1}"

echo "🧹 Cleaning up orphaned AWS resources for environment: $ENV"
echo "Project: $PROJECT"
echo "Region: $REGION"
echo ""

# VPC Connector
echo "🔍 Checking VPC Connector..."
VPC_CONN_ARN=$(aws apprunner list-vpc-connectors \
  --region "$REGION" \
  --query "VpcConnectors[?VpcConnectorName=='${PROJECT}-${ENV}-vpc-connector'].VpcConnectorArn | [0]" \
  --output text 2>/dev/null || echo "")

if [ -n "$VPC_CONN_ARN" ] && [ "$VPC_CONN_ARN" != "None" ]; then
  echo "🗑️ Deleting VPC Connector: $VPC_CONN_ARN"
  aws apprunner delete-vpc-connector --vpc-connector-arn "$VPC_CONN_ARN" --region "$REGION"
  echo "✅ VPC Connector deleted"
else
  echo "✓ VPC Connector not found"
fi

# IAM Roles
echo ""
echo "🔍 Checking IAM Roles..."
for role in "app-runner-ecr-role" "app-runner-instance-role"; do
  ROLE_NAME="${PROJECT}-${ENV}-${role}"
  
  if aws iam get-role --role-name "$ROLE_NAME" 2>/dev/null; then
    echo "🗑️ Detaching policies and deleting IAM role: $ROLE_NAME"
    
    # Detach managed policies
    ATTACHED_POLICIES=$(aws iam list-attached-role-policies --role-name "$ROLE_NAME" --query 'AttachedPolicies[].PolicyArn' --output text 2>/dev/null || echo "")
    for policy_arn in $ATTACHED_POLICIES; do
      echo "  - Detaching policy: $policy_arn"
      aws iam detach-role-policy --role-name "$ROLE_NAME" --policy-arn "$policy_arn" || true
    done
    
    # Delete inline policies
    INLINE_POLICIES=$(aws iam list-role-policies --role-name "$ROLE_NAME" --query 'PolicyNames[]' --output text 2>/dev/null || echo "")
    for policy_name in $INLINE_POLICIES; do
      echo "  - Deleting inline policy: $policy_name"
      aws iam delete-role-policy --role-name "$ROLE_NAME" --policy-name "$policy_name" || true
    done
    
    # Delete role
    aws iam delete-role --role-name "$ROLE_NAME"
    echo "✅ IAM role deleted: $ROLE_NAME"
  else
    echo "✓ IAM role not found: $ROLE_NAME"
  fi
done

# IAM Policy
echo ""
echo "🔍 Checking IAM Policy..."
POLICY_NAME="${PROJECT}-${ENV}-secrets-access"
POLICY_ARN=$(aws iam list-policies \
  --query "Policies[?PolicyName=='$POLICY_NAME'].Arn | [0]" \
  --output text 2>/dev/null || echo "")

if [ -n "$POLICY_ARN" ] && [ "$POLICY_ARN" != "None" ]; then
  echo "🗑️ Deleting IAM policy: $POLICY_NAME"
  aws iam delete-policy --policy-arn "$POLICY_ARN"
  echo "✅ IAM policy deleted"
else
  echo "✓ IAM policy not found"
fi

# Secrets Manager
echo ""
echo "🔍 Checking Secrets Manager..."
SECRET_NAME="${PROJECT}-${ENV}-db-password"
if aws secretsmanager describe-secret --secret-id "$SECRET_NAME" --region "$REGION" 2>/dev/null; then
  echo "🗑️ Deleting secret: $SECRET_NAME"
  aws secretsmanager delete-secret \
    --secret-id "$SECRET_NAME" \
    --force-delete-without-recovery \
    --region "$REGION"
  echo "✅ Secret deleted"
else
  echo "✓ Secret not found"
fi

# DB Subnet Group
echo ""
echo "🔍 Checking DB Subnet Group..."
SUBNET_GROUP="${PROJECT}-${ENV}-db-subnet-group"
if aws rds describe-db-subnet-groups --db-subnet-group-name "$SUBNET_GROUP" --region "$REGION" 2>/dev/null; then
  echo "🗑️ Deleting DB subnet group: $SUBNET_GROUP"
  aws rds delete-db-subnet-group --db-subnet-group-name "$SUBNET_GROUP" --region "$REGION"
  echo "✅ DB subnet group deleted"
else
  echo "✓ DB subnet group not found"
fi

# S3 Buckets
echo ""
echo "🔍 Checking S3 Buckets..."
for bucket_suffix in "frontend" "benchmark-raw" "benchmark-curated"; do
  BUCKET_NAME="${PROJECT}-${ENV}-${bucket_suffix}"
  
  if aws s3api head-bucket --bucket "$BUCKET_NAME" --region "$REGION" 2>/dev/null; then
    echo "🗑️ Emptying and deleting bucket: $BUCKET_NAME"
    aws s3 rm "s3://$BUCKET_NAME" --recursive --region "$REGION" || true
    aws s3 rb "s3://$BUCKET_NAME" --region "$REGION"
    echo "✅ Bucket deleted: $BUCKET_NAME"
  else
    echo "✓ Bucket not found: $BUCKET_NAME"
  fi
done

# CloudFront OAC
echo ""
echo "🔍 Checking CloudFront Origin Access Control..."
OAC_ID=$(aws cloudfront list-origin-access-controls \
  --query "OriginAccessControlList.Items[?Name=='${PROJECT}-${ENV}-oac'].Id | [0]" \
  --output text 2>/dev/null || echo "")

if [ -n "$OAC_ID" ] && [ "$OAC_ID" != "None" ]; then
  echo "🗑️ Deleting CloudFront OAC: $OAC_ID"
  ETAG=$(aws cloudfront get-origin-access-control --id "$OAC_ID" --query 'ETag' --output text)
  aws cloudfront delete-origin-access-control --id "$OAC_ID" --if-match "$ETAG"
  echo "✅ CloudFront OAC deleted"
else
  echo "✓ CloudFront OAC not found"
fi

echo ""
echo "✅ Cleanup complete!"
echo ""
echo "You can now run a fresh Terraform apply:"
echo "  cd terraform/deploy"
echo "  terraform init -reconfigure"
echo "  terraform apply -var-file=\"environments/$ENV.tfvars\""






