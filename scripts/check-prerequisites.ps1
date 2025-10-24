# Check prerequisites for AWS setup
Write-Host "Checking prerequisites..."

$errors = @()

# Check AWS CLI
try {
    $awsVersion = aws --version 2>$null
    if ($awsVersion) {
        Write-Host "✓ AWS CLI: $awsVersion"
    }
    else {
        $errors += "AWS CLI not found"
    }
}
catch {
    $errors += "AWS CLI not found"
}

# Check Terraform
try {
    $terraformVersion = terraform --version 2>$null
    if ($terraformVersion) {
        Write-Host "✓ Terraform: $terraformVersion"
    }
    else {
        $errors += "Terraform not found"
    }
}
catch {
    $errors += "Terraform not found"
}

# Check Docker
try {
    $dockerVersion = docker --version 2>$null
    if ($dockerVersion) {
        Write-Host "✓ Docker: $dockerVersion"
    }
    else {
        $errors += "Docker not found"
    }
}
catch {
    $errors += "Docker not found"
}

# Check AWS credentials
try {
    $awsIdentity = aws sts get-caller-identity 2>$null
    if ($awsIdentity) {
        $identity = $awsIdentity | ConvertFrom-Json
        Write-Host "✓ AWS Account: $($identity.Account)"
        Write-Host "✓ AWS User: $($identity.Arn)"
    }
    else {
        $errors += "AWS credentials not configured"
    }
}
catch {
    $errors += "AWS credentials not configured"
}

if ($errors.Count -gt 0) {
    Write-Host "`n❌ Prerequisites check failed:"
    $errors | ForEach-Object { Write-Host "  - $_" }
    exit 1
}
else {
    Write-Host "`n✓ All prerequisites met!"
}





