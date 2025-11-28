# Test fixed geometry generation
$baseUrl = "http://localhost:5004/api/v1"

Write-Host "=== Testing Fixed Geometry Generation ===" -ForegroundColor Cyan

# Create a new mission case with unique name
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$missionCaseDto = @{
    name = "Test Fixed Geometry $timestamp"
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
    Write-Host "Creating mission case..." -ForegroundColor Yellow
    $missionCase = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/mission-cases" -Method POST -ContentType "application/json" -Body ($missionCaseDto | ConvertTo-Json -Depth 10)
    Write-Host "  Mission Case ID: $($missionCase.id)" -ForegroundColor Green

    # Create sizing run
    $sizingRunDto = @{
        MissionCaseId = $missionCase.id
        VesselCategory = "container"
        VesselType = "500 TEU Container Feeder"
        BowFamily = "bulbous"
        MidshipFamily = "u"
        SternFamily = "transom"
        SolverMode = "parametric"
    }

    Write-Host "Creating sizing run..." -ForegroundColor Yellow
    $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs" -Method POST -ContentType "application/json" -Body ($sizingRunDto | ConvertTo-Json -Depth 10)
    Write-Host "  Run ID: $($run.id)" -ForegroundColor Green

    # Wait for completion
    $maxWait = 30
    $waited = 0
    while ($run.runStatus -ne "completed" -and $run.runStatus -ne "failed" -and $waited -lt $maxWait) {
        Start-Sleep -Seconds 2
        $waited += 2
        $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)" -Method GET
        Write-Host "  Status: $($run.runStatus) (waited $waited s)" -ForegroundColor Gray
    }

    if ($run.runStatus -ne "completed") {
        Write-Host "  ❌ Run failed or timed out" -ForegroundColor Red
        exit 1
    }

    # Fetch candidates
    Write-Host "`nFetching candidates..." -ForegroundColor Yellow
    $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)/candidates" -Method GET
    Write-Host "  Found $($candidates.Count) candidates`n" -ForegroundColor Green

    # Analyze ShipD vectors
    Write-Host "=== ShipD Vector Analysis ===" -ForegroundColor Cyan
    for ($i = 0; $i -lt $candidates.Count; $i++) {
        $c = $candidates[$i]
        $rank = $i + 1

        Write-Host "`nCandidate ${rank}:" -ForegroundColor Yellow
        Write-Host "  Dimensions: Lpp=$($c.lppM)m, Beam=$($c.beamM)m, Draft=$($c.draftM)m, Cb=$($c.cb)" -ForegroundColor White

        if (![string]::IsNullOrEmpty($c.shipdParametersJson)) {
            try {
                $vector = $c.shipdParametersJson | ConvertFrom-Json
                $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
                Write-Host "  ShipD Vector: $nonZeroCount/45 non-zero parameters" -ForegroundColor $(if ($nonZeroCount -gt 5) { "Green" } else { "Yellow" })
                Write-Host "    Vector[1] (Bow Ratio): $($vector[1])" -ForegroundColor $(if ($vector[1] -gt 0) { "Green" } else { "Red" })
                Write-Host "    Vector[2] (Stern Ratio): $($vector[2])" -ForegroundColor $(if ($vector[2] -gt 0) { "Green" } else { "Red" })
                Write-Host "    Vector[20] (Service Speed): $($vector[20])" -ForegroundColor $(if ($vector[20] -gt 0) { "Green" } else { "Red" })
                Write-Host "    Vector[31] (bit_BB): $($vector[31])" -ForegroundColor $(if ($vector[31] -gt 0) { "Green" } else { "Gray" })

                if ($nonZeroCount -le 3) {
                    Write-Host "    ⚠️ WARNING: ShipD vector is mostly zeros!" -ForegroundColor Yellow
                } else {
                    Write-Host "    ✅ ShipD vector is properly populated" -ForegroundColor Green
                }
            } catch {
                Write-Host "    ⚠️ Failed to parse ShipD vector: $_" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  ❌ No ShipD parameters" -ForegroundColor Red
        }

        Write-Host "  Geometry Status: $($c.geometryGenerationStatus)" -ForegroundColor $(if ($c.geometryGenerationStatus -eq "Success") { "Green" } else { "Yellow" })
        Write-Host "  Has geometryJson: $(![string]::IsNullOrEmpty($c.geometryJson))" -ForegroundColor $(if (![string]::IsNullOrEmpty($c.geometryJson)) { "Green" } else { "Yellow" })
    }

    Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
    Write-Host "Run ID: $($run.id)" -ForegroundColor Gray

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}
