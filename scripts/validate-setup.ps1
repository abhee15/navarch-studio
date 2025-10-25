# Validate AWS setup
Write-Host "🔍 Validating AWS setup..." -ForegroundColor Yellow

# Check S3 bucket
Write-Host "Checking S3 bucket..."
$bucketName = "navarch-studio-terraform-state"
$bucketExists = aws s3 ls "s3://$bucketName" 2>$null
if ($bucketExists) {
    Write-Host "✓ S3 bucket exists" -ForegroundColor Green
}
else {
    Write-Host "❌ S3 bucket not found" -ForegroundColor Red
}

# Check DynamoDB table
Write-Host "Checking DynamoDB table..."
$tableName = "navarch-studio-terraform-locks"
$tableExists = aws dynamodb describe-table --table-name $tableName 2>$null
if ($tableExists) {
    Write-Host "✓ DynamoDB table exists" -ForegroundColor Green
}
else {
    Write-Host "❌ DynamoDB table not found" -ForegroundColor Red
}

# Check ECR repositories
Write-Host "Checking ECR repositories..."
$repositories = @("identity-service", "api-gateway", "data-service", "frontend")
foreach ($repo in $repositories) {
    $repoName = "navarch-studio-$repo"
    $repoExists = aws ecr describe-repositories --repository-names $repoName 2>$null
    if ($repoExists) {
        Write-Host "✓ ECR repository $repoName exists" -ForegroundColor Green
    }
    else {
        Write-Host "❌ ECR repository $repoName not found" -ForegroundColor Red
    }
}

# Check VPC
Write-Host "Checking VPC..."
$vpcId = aws ec2 describe-vpcs --filters "Name=tag:Name,Values=navarch-studio-vpc" --query "Vpcs[0].VpcId" --output text
if ($vpcId -and $vpcId -ne "None") {
    Write-Host "✓ VPC exists: $vpcId" -ForegroundColor Green
}
else {
    Write-Host "❌ VPC not found" -ForegroundColor Red
}

# Check Cognito
Write-Host "Checking Cognito User Pool..."
$userPoolId = aws cognito-idp list-user-pools --max-items 10 --query "UserPools[?Name=='navarch-studio-user-pool'].Id" --output text
if ($userPoolId) {
    Write-Host "✓ Cognito User Pool exists: $userPoolId" -ForegroundColor Green
}
else {
    Write-Host "❌ Cognito User Pool not found" -ForegroundColor Red
}

Write-Host "`n✅ Validation completed!" -ForegroundColor Green





