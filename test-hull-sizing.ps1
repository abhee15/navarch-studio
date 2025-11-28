# Test Hull Sizing Functionality
Write-Host "=== Testing Hull Sizing Functionality ===" -ForegroundColor Cyan

# Step 1: Create a Mission Case
Write-Host "`n[1] Creating Mission Case..." -ForegroundColor Yellow
$missionCaseJson = @{
    name               = "Test Container Ship"
    missionCategory    = "commercial"
    missionType        = "container"
    cargoBasis         = "teu"
    teuCount           = 5000
    cargoValue         = 5000
    cargoDensityTPerM3 = 0.5
    serviceSpeedKn     = 20.0
    seaMarginPct       = 15.0
    bowFamily          = "bulbous"
    midshipFamily      = "u"
    sternFamily        = "transom"
} | ConvertTo-Json

try {
    $missionCaseResponse = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/mission-cases" `
        -Method POST `
        -Body $missionCaseJson `
        -ContentType "application/json"

    $missionCaseId = $missionCaseResponse.id
    Write-Host "✅ Mission Case Created: $missionCaseId" -ForegroundColor Green
    Write-Host "   Name: $($missionCaseResponse.name)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Failed to create mission case: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
    exit 1
}

# Step 2: Create a Sizing Run
Write-Host "`n[2] Creating Sizing Run..." -ForegroundColor Yellow
$sizingRunJson = @{
    missionCaseId = $missionCaseId
    mode          = "first_principles"
    options       = @{
        maxCandidates   = 5
        includeGeometry = $true
    }
} | ConvertTo-Json -Depth 3

try {
    $sizingRunResponse = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs" `
        -Method POST `
        -Body $sizingRunJson `
        -ContentType "application/json"

    $sizingRunId = $sizingRunResponse.id
    Write-Host "✅ Sizing Run Created: $sizingRunId" -ForegroundColor Green
    Write-Host "   Status: $($sizingRunResponse.runStatus)" -ForegroundColor Gray
    Write-Host "   Mode: $($sizingRunResponse.mode)" -ForegroundColor Gray
    Write-Host "   Candidate Count: $($sizingRunResponse.candidateCount)" -ForegroundColor Gray

    if ($sizingRunResponse.computeTimeMs) {
        Write-Host "   Compute Time: $($sizingRunResponse.computeTimeMs)ms" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Failed to create sizing run: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

# Step 3: Wait a bit and check candidates
Write-Host "`n[3] Waiting for candidates to be generated..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

try {
    $candidatesResponse = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs/$sizingRunId/candidates" `
        -Method GET

    Write-Host "✅ Found $($candidatesResponse.Count) candidates" -ForegroundColor Green

    if ($candidatesResponse.Count -gt 0) {
        Write-Host "`nFirst Candidate Details:" -ForegroundColor Cyan
        $firstCandidate = $candidatesResponse[0]
        Write-Host "   LPP: $($firstCandidate.lpp)m" -ForegroundColor Gray
        Write-Host "   Beam: $($firstCandidate.beam)m" -ForegroundColor Gray
        Write-Host "   Draft: $($firstCandidate.draft)m" -ForegroundColor Gray
        Write-Host "   Displacement: $($firstCandidate.displacementT)t" -ForegroundColor Gray
        Write-Host "   Block Coefficient: $($firstCandidate.blockCoefficient)" -ForegroundColor Gray
        if ($firstCandidate.vesselId) {
            Write-Host "   Vessel ID: $($firstCandidate.vesselId)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "⚠️  No candidates generated" -ForegroundColor Yellow
        if ($sizingRunResponse.diagnostics) {
            Write-Host "   Diagnostics: $($sizingRunResponse.diagnostics.summary)" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "⚠️  Could not fetch candidates: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 4: Verify the sizing run status
Write-Host "`n[4] Checking Sizing Run Status..." -ForegroundColor Yellow
try {
    $runStatus = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs/$sizingRunId" `
        -Method GET

    Write-Host "✅ Sizing Run Status: $($runStatus.runStatus)" -ForegroundColor Green
    Write-Host "   Candidate Count: $($runStatus.candidateCount)" -ForegroundColor Gray

    if ($runStatus.errorMessage) {
        Write-Host "   Error: $($runStatus.errorMessage)" -ForegroundColor Red
    }
}
catch {
    Write-Host "⚠️  Could not fetch run status: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
