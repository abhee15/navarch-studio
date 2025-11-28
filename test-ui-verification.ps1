# UI Verification Test Script
# Creates a test run and provides instructions for manual UI verification

$baseUrl = "http://localhost:5004/api/v1"

Write-Host "=== UI Verification Test Setup ===" -ForegroundColor Cyan

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$missionCaseDto = @{
    name = "UI Verification Test $timestamp"
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
    Write-Host "Creating mission case and sizing run..." -ForegroundColor Yellow
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

    Write-Host "`n✅ Test Run Created Successfully!" -ForegroundColor Green
    Write-Host "`n=== Test Run Details ===" -ForegroundColor Cyan
    Write-Host "Run ID: $($run.id)" -ForegroundColor White
    Write-Host "Mission Case ID: $($missionCase.id)" -ForegroundColor White
    Write-Host "Candidates Generated: $($candidates.Count)" -ForegroundColor White

    Write-Host "`n=== Candidate Details ===" -ForegroundColor Cyan
    for ($i = 0; $i -lt $candidates.Count; $i++) {
        $c = $candidates[$i]
        $rank = $i + 1
        Write-Host "`nCandidate ${rank}:" -ForegroundColor Yellow
        Write-Host "  ID: $($c.id)" -ForegroundColor Gray
        Write-Host "  Dimensions: Lpp=$($c.lppM)m, Beam=$($c.beamM)m, Draft=$($c.draftM)m" -ForegroundColor White
        Write-Host "  Cb=$($c.cb), Displacement=$($c.dispT)t" -ForegroundColor White

        if (![string]::IsNullOrEmpty($c.shipdParametersJson)) {
            $vector = $c.shipdParametersJson | ConvertFrom-Json
            $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
            Write-Host "  ShipD Vector: $nonZeroCount/45 non-zero" -ForegroundColor $(if ($nonZeroCount -gt 5) { "Green" } else { "Yellow" })
            Write-Host "    Vector[1]=$($vector[1]), Vector[2]=$($vector[2]), Vector[31]=$($vector[31])" -ForegroundColor Gray
        }

        Write-Host "  Workspace URL: http://localhost:3000/sizing/candidates/$($c.id)" -ForegroundColor Cyan
    }

    Write-Host "`n=== UI Verification Checklist ===" -ForegroundColor Cyan
    Write-Host "`n1. Open Frontend:" -ForegroundColor Yellow
    Write-Host "   URL: http://localhost:3000" -ForegroundColor White
    Write-Host "   Login: abhee15@gmail.com / Abhishikth12345`$" -ForegroundColor White

    Write-Host "`n2. Navigate to Sizing Run:" -ForegroundColor Yellow
    Write-Host "   - Go to Hull Sizing section" -ForegroundColor White
    Write-Host "   - Find run: $($run.id)" -ForegroundColor White
    Write-Host "   - Or search for mission case: $($missionCase.name)" -ForegroundColor White

    Write-Host "`n3. For EACH of the 5 candidates:" -ForegroundColor Yellow
    Write-Host "   a) Open candidate workspace" -ForegroundColor White
    Write-Host "   b) Check 3D Isometric Panel:" -ForegroundColor White
    Write-Host "      - Does hull shape look correct for container ship?" -ForegroundColor Gray
    Write-Host "      - Is the shape unique (different from other candidates)?" -ForegroundColor Gray
    Write-Host "      - Are proportions correct (Lpp, Beam, Draft visible)?" -ForegroundColor Gray
    Write-Host "      - If bulbous bow: Is bulb visible?" -ForegroundColor Gray
    Write-Host "   c) Check 2D Plan View:" -ForegroundColor White
    Write-Host "      - Does it match 3D view (top-down)?" -ForegroundColor Gray
    Write-Host "      - Are waterlines visible and correct?" -ForegroundColor Gray
    Write-Host "   d) Check 2D Profile View:" -ForegroundColor White
    Write-Host "      - Does it match 3D view (side)?" -ForegroundColor Gray
    Write-Host "      - Are buttocks visible and correct?" -ForegroundColor Gray
    Write-Host "   e) Check 2D Sections View:" -ForegroundColor White
    Write-Host "      - Does it match 3D view (body plan)?" -ForegroundColor Gray
    Write-Host "      - Are sections smooth and realistic?" -ForegroundColor Gray
    Write-Host "   f) Test Parameter Adjustment:" -ForegroundColor White
    Write-Host "      - Adjust Lpp, Beam, Draft, or Cb" -ForegroundColor Gray
    Write-Host "      - Does geometry update smoothly?" -ForegroundColor Gray
    Write-Host "      - Does geometry switch or flicker?" -ForegroundColor Gray
    Write-Host "      - Do all panels update consistently?" -ForegroundColor Gray

    Write-Host "`n4. Compare Candidates:" -ForegroundColor Yellow
    Write-Host "   - Are candidates 1-2 visually similar?" -ForegroundColor White
    Write-Host "   - Are candidates 3-5 visually different from 1-2?" -ForegroundColor White
    Write-Host "   - Do differences match numerical differences (Cb, Lpp)?" -ForegroundColor White

    Write-Host "`n=== Expected Results ===" -ForegroundColor Cyan
    Write-Host "✅ Each candidate should have unique visual appearance" -ForegroundColor Green
    Write-Host "✅ 3D isometric should show correct hull shape for container ship" -ForegroundColor Green
    Write-Host "✅ All 2D panels should match 3D view" -ForegroundColor Green
    Write-Host "✅ Geometry should NOT switch when opening workspace" -ForegroundColor Green
    Write-Host "✅ Parameter adjustments should update smoothly" -ForegroundColor Green
    Write-Host "✅ Candidates 1-2 should be similar (lower Cb)" -ForegroundColor Green
    Write-Host "✅ Candidates 3-5 should be different (higher Cb, longer Lpp)" -ForegroundColor Green

    Write-Host "`n=== Test Data Saved ===" -ForegroundColor Cyan
    Write-Host "Run ID: $($run.id)" -ForegroundColor White
    Write-Host "Mission Case ID: $($missionCase.id)" -ForegroundColor White
    Write-Host "`nCandidate IDs:" -ForegroundColor White
    for ($i = 0; $i -lt $candidates.Count; $i++) {
        Write-Host "  Candidate $($i+1): $($candidates[$i].id)" -ForegroundColor Gray
    }

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
