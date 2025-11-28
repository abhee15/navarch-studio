# Test workspace geometry for each candidate
# Opens workspace and verifies geometry is displayed correctly

$baseUrl = "http://localhost:5004/api/v1"

Write-Host "`n=== Workspace Geometry Verification ===" -ForegroundColor Cyan

# Use the latest run from the comprehensive test
# For now, let's create a fresh run to test
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$missionCaseDto = @{
    name = "Workspace Test $timestamp"
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
    Write-Host "Creating test mission case and run..." -ForegroundColor Yellow
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
        Write-Host "Run failed or timed out" -ForegroundColor Red
        exit 1
    }

    # Fetch candidates
    $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)/candidates" -Method GET

    Write-Host "`nFound $($candidates.Count) candidates. Testing workspace geometry for each...`n" -ForegroundColor Green

    foreach ($candidate in $candidates) {
        $rank = $candidates.IndexOf($candidate) + 1

        Write-Host "=== Candidate $rank (ID: $($candidate.id)) ===" -ForegroundColor Cyan
        Write-Host "Dimensions: Lpp=$($candidate.lppM)m, Beam=$($candidate.beamM)m, Draft=$($candidate.draftM)m" -ForegroundColor White
        Write-Host "Cb=$($candidate.cb), Displacement=$($candidate.dispT)t" -ForegroundColor White

        # Check ShipD vector
        if (![string]::IsNullOrEmpty($candidate.shipdParametersJson)) {
            $vector = $candidate.shipdParametersJson | ConvertFrom-Json
            $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
            Write-Host "ShipD Vector: $nonZeroCount/45 non-zero, Vector[1]=$($vector[1]), Vector[2]=$($vector[2])" -ForegroundColor $(if ($nonZeroCount -gt 5) { "Green" } else { "Yellow" })
        }

        # Check geometry
        $hasGeometry = ![string]::IsNullOrEmpty($candidate.geometryJson)
        Write-Host "Geometry Status: $($candidate.geometryGenerationStatus), Has geometry: $hasGeometry" -ForegroundColor $(if ($hasGeometry) { "Green" } else { "Yellow" })

        if ($hasGeometry) {
            try {
                $geometry = $candidate.geometryJson | ConvertFrom-Json

                # Check geometry format
                if ($geometry.stations) {
                    Write-Host "  Geometry Format: ShipD Sections" -ForegroundColor Gray
                    Write-Host "  Stations: $($geometry.stations.Count)" -ForegroundColor Gray
                } elseif ($geometry.offsets) {
                    Write-Host "  Geometry Format: OffsetsGrid" -ForegroundColor Gray
                    Write-Host "  Stations: $($geometry.stations.Count), Waterlines: $($geometry.waterlines.Count)" -ForegroundColor Gray
                } else {
                    Write-Host "  Geometry Format: Unknown" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "  ⚠️ Failed to parse geometry JSON" -ForegroundColor Yellow
            }
        }

        # Get candidate details (simulating workspace open)
        Write-Host "  Workspace URL: http://localhost:5173/sizing/candidates/$($candidate.id)" -ForegroundColor Gray
        Write-Host ""
    }

    Write-Host "=== Workspace Test Complete ===" -ForegroundColor Cyan
    Write-Host "Run ID: $($run.id)" -ForegroundColor Gray
    Write-Host "`nTo verify in UI:" -ForegroundColor Yellow
    Write-Host "1. Open http://localhost:5173" -ForegroundColor White
    Write-Host "2. Navigate to the sizing run" -ForegroundColor White
    Write-Host "3. Open each candidate workspace" -ForegroundColor White
    Write-Host "4. Verify 3D isometric panel shows correct hull shape" -ForegroundColor White
    Write-Host "5. Verify 2D panels (Plan, Profile, Sections) match 3D view" -ForegroundColor White
    Write-Host "6. Verify geometry updates when adjusting parameters" -ForegroundColor White

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
