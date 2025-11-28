# Test fix for Candidate 1 - ensure it gets family defaults applied
# The issue is that Candidate 1 only gets defaults (0.30/0.30) but not family-specific parameters

$baseUrl = "http://localhost:5004/api/v1"

Write-Host "=== Testing Candidate 1 Fix ===" -ForegroundColor Cyan

# Create a test run
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$missionCaseDto = @{
    name = "Candidate 1 Fix Test $timestamp"
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

    # Wait for completion
    $waited = 0
    while ($run.runStatus -ne "completed" -and $run.runStatus -ne "failed" -and $waited -lt 60) {
        Start-Sleep -Seconds 2
        $waited += 2
        $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)" -Method GET
    }

    if ($run.runStatus -ne "completed") {
        Write-Host "Run failed" -ForegroundColor Red
        exit 1
    }

    # Get candidates
    $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)/candidates" -Method GET

    Write-Host "`n=== Candidate 1 Analysis ===" -ForegroundColor Yellow
    $c1 = $candidates[0]

    Write-Host "Dimensions: Lpp=$($c1.lppM)m, Beam=$($c1.beamM)m, Draft=$($c1.draftM)m" -ForegroundColor White
    Write-Host "Cb=$($c1.cb), BowFamily=$($c1.bowFamily)" -ForegroundColor White

    if (![string]::IsNullOrEmpty($c1.shipdParametersJson)) {
        $vector = $c1.shipdParametersJson | ConvertFrom-Json
        $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count

        Write-Host "`nShipD Vector Analysis:" -ForegroundColor Cyan
        Write-Host "  Non-zero params: $nonZeroCount/45" -ForegroundColor White
        Write-Host "  Vector[1] (Bow): $($vector[1])" -ForegroundColor $(if ($vector[1] -gt 0) { "Green" } else { "Red" })
        Write-Host "  Vector[2] (Stern): $($vector[2])" -ForegroundColor $(if ($vector[2] -gt 0) { "Green" } else { "Red" })
        Write-Host "  Vector[31] (bit_BB): $($vector[31])" -ForegroundColor $(if ($vector[31] -gt 0.5) { "Green" } else { "Yellow" })

        # Check if bulbous bow flag should be set
        if ($c1.bowFamily -eq "bulbous" -and $vector[31] -lt 0.5) {
            Write-Host "  ⚠️ ISSUE: Bulbous bow family selected but bit_BB is not set!" -ForegroundColor Yellow
        }

        # List all non-zero parameters
        Write-Host "`n  Non-zero parameters:" -ForegroundColor Cyan
        for ($i = 0; $i -lt $vector.Length; $i++) {
            if ($vector[$i] -ne 0) {
                Write-Host "    Vector[$i] = $($vector[$i])" -ForegroundColor Gray
            }
        }
    }

    Write-Host "`n=== Comparison: Candidate 1 vs Candidate 2 ===" -ForegroundColor Yellow
    $c2 = $candidates[1]

    if (![string]::IsNullOrEmpty($c2.shipdParametersJson)) {
        $vector2 = $c2.shipdParametersJson | ConvertFrom-Json
        $nonZeroCount2 = ($vector2 | Where-Object { $_ -ne 0 }).Count

        Write-Host "Candidate 2:" -ForegroundColor Cyan
        Write-Host "  Non-zero params: $nonZeroCount2/45" -ForegroundColor White
        Write-Host "  Vector[1] (Bow): $($vector2[1])" -ForegroundColor Green
        Write-Host "  Vector[2] (Stern): $($vector2[2])" -ForegroundColor Green
        Write-Host "  Vector[31] (bit_BB): $($vector2[31])" -ForegroundColor White

        Write-Host "`nDifference:" -ForegroundColor Cyan
        Write-Host "  Candidate 1 has $($nonZeroCount) non-zero params vs Candidate 2 has $($nonZeroCount2)" -ForegroundColor White
        Write-Host "  This is expected - Candidate 1 is the base, Candidate 2 is adjusted" -ForegroundColor Gray
    }

    Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
    Write-Host "Run ID: $($run.id)" -ForegroundColor Gray
    Write-Host "Candidate 1 ID: $($c1.id)" -ForegroundColor Gray

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
