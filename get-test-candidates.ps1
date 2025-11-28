# Quick script to get candidate IDs for UI testing
$baseUrl = "http://localhost:5004/api/v1"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "Creating test run..." -ForegroundColor Yellow

$missionCaseDto = @{
    name = "UI Test $timestamp"
    missionCategory = "commercial"
    missionType = "container"
    cargoBasis = "teu"
    cargoValue = 500
    teuCount = 500
    serviceSpeedKn = 18.0
    bowFamily = "bulbous"
    midshipFamily = "u"
    sternFamily = "transom"
}

try {
    $missionCase = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/mission-cases" -Method POST -ContentType "application/json" -Body ($missionCaseDto | ConvertTo-Json -Depth 10)

    $sizingRunDto = @{
        MissionCaseId = $missionCase.id
        VesselCategory = "container"
        VesselType = "500 TEU Container Feeder"
        BowFamily = "bulbous"
        MidshipFamily = "u"
        SternFamily = "transom"
        SolverMode = "parametric"
    }

    $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs" -Method POST -ContentType "application/json" -Body ($sizingRunDto | ConvertTo-Json -Depth 10)

    Write-Host "Waiting for run completion..." -ForegroundColor Yellow
    $waited = 0
    while ($run.runStatus -ne "completed" -and $run.runStatus -ne "failed" -and $waited -lt 60) {
        Start-Sleep -Seconds 2
        $waited += 2
        $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)" -Method GET
    }

    if ($run.runStatus -ne "completed") {
        Write-Host "Run failed or timed out" -ForegroundColor Red
        exit 1
    }

    $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)/candidates" -Method GET

    Write-Host "`n=== Test Run Created ===" -ForegroundColor Green
    Write-Host "Run ID: $($run.id)" -ForegroundColor White
    Write-Host "Candidates: $($candidates.Count)" -ForegroundColor White

    Write-Host "`n=== Candidate Workspace URLs ===" -ForegroundColor Cyan
    for ($i = 0; $i -lt $candidates.Count; $i++) {
        $c = $candidates[$i]
        $rank = $i + 1
        Write-Host "Candidate $rank:" -ForegroundColor Yellow
        Write-Host "  URL: http://localhost:3000/sizing/workspace/$($c.id)" -ForegroundColor White
        Write-Host "  Dimensions: Lpp=$($c.lppM)m, Beam=$($c.beamM)m, Draft=$($c.draftM)m" -ForegroundColor Gray
        Write-Host "  Cb=$($c.cb), Displacement=$($c.dispT)t" -ForegroundColor Gray
    }

    # Output first candidate ID for direct navigation
    Write-Host "`n=== First Candidate for Testing ===" -ForegroundColor Cyan
    Write-Host "Candidate 1 ID: $($candidates[0].id)" -ForegroundColor White
    Write-Host "Direct URL: http://localhost:3000/sizing/workspace/$($candidates[0].id)" -ForegroundColor Green

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
