# Comprehensive Geometry Investigation Script
# Tests multiple vessel types and analyzes geometry generation

$baseUrl = "http://localhost:5004/api/v1"

Write-Host "`n=== Hull Sizing Geometry Investigation ===" -ForegroundColor Cyan
Write-Host "Testing multiple vessel types to identify geometry issues`n" -ForegroundColor Yellow

# Test vessel types
$vesselTypes = @(
    @{
        Name = "500 TEU Container Feeder"
        Category = "container"
        Type = "500 TEU Container Feeder"
        CargoBasis = "TEU"
        CargoValue = 500
        ServiceSpeedKn = 18
        BowFamily = "bulbous"
        MidshipFamily = "u"
        SternFamily = "transom"
    },
    @{
        Name = "50K DWT Bulk Carrier"
        Category = "bulk"
        Type = "50K DWT Bulk Carrier"
        CargoBasis = "Weight"
        CargoValue = 50000
        ServiceSpeedKn = 14
        BowFamily = "straight"
        MidshipFamily = "u"
        SternFamily = "transom"
    },
    @{
        Name = "10K DWT General Cargo"
        Category = "general_cargo"
        Type = "10K DWT General Cargo"
        CargoBasis = "Weight"
        CargoValue = 10000
        ServiceSpeedKn = 15
        BowFamily = "straight"
        MidshipFamily = "u"
        SternFamily = "transom"
    }
)

$results = @()

foreach ($vessel in $vesselTypes) {
    Write-Host "`n--- Testing: $($vessel.Name) ---" -ForegroundColor Green

    # Get or create mission case
    $missionCaseName = "Test - $($vessel.Name) $(Get-Date -Format 'HHmmss')"
    $missionCaseDto = @{
        name = $missionCaseName
        missionCategory = "commercial"  # Must be: commercial, government, recreational, research
        missionType = $vessel.Type
        cargoBasis = $vessel.CargoBasis
        cargoValue = $vessel.CargoValue
        serviceSpeedKn = $vessel.ServiceSpeedKn
        bowFamily = $vessel.BowFamily
        midshipFamily = $vessel.MidshipFamily
        sternFamily = $vessel.SternFamily
    }

    try {
        Write-Host "Creating mission case..." -ForegroundColor Gray
        try {
            $missionCase = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/mission-cases" -Method POST -ContentType "application/json" -Body ($missionCaseDto | ConvertTo-Json -Depth 10)
            Write-Host "  Mission Case ID: $($missionCase.id)" -ForegroundColor Gray
        } catch {
            if ($_.Exception.Response.StatusCode -eq 409) {
                # Mission case already exists, try to find it
                Write-Host "  Mission case exists, fetching..." -ForegroundColor Yellow
                $existingCases = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/mission-cases" -Method GET
                $missionCase = $existingCases | Where-Object { $_.name -like "Test - $($vessel.Name)*" } | Select-Object -First 1
                if ($missionCase) {
                    Write-Host "  Using existing Mission Case ID: $($missionCase.id)" -ForegroundColor Gray
                } else {
                    throw "Could not find existing mission case"
                }
            } else {
                throw
            }
        }

        # Create sizing run
        $sizingRunDto = @{
            MissionCaseId = $missionCase.id
            VesselCategory = $vessel.Category
            VesselType = $vessel.Type
            BowFamily = $vessel.BowFamily
            MidshipFamily = $vessel.MidshipFamily
            SternFamily = $vessel.SternFamily
            SolverMode = "parametric"
        }

        Write-Host "Creating sizing run..." -ForegroundColor Gray
        $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs" -Method POST -ContentType "application/json" -Body ($sizingRunDto | ConvertTo-Json -Depth 10)
        Write-Host "  Run ID: $($run.id)" -ForegroundColor Gray

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
            Write-Host "  ❌ Run failed or timed out (status: $($run.runStatus))" -ForegroundColor Red
            continue
        }

        # Fetch candidates
        Write-Host "Fetching candidates..." -ForegroundColor Gray
        $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)/candidates" -Method GET

        Write-Host "  Generated $($candidates.Count) candidates" -ForegroundColor Green

        # Analyze each candidate
        for ($i = 0; $i -lt $candidates.Count; $i++) {
            $candidate = $candidates[$i]
            $rank = $i + 1

            Write-Host "`n  Candidate ${rank}:" -ForegroundColor Cyan
            Write-Host "    Lpp: $($candidate.lppM) m, Beam: $($candidate.beamM) m, Draft: $($candidate.draftM) m" -ForegroundColor White
            Write-Host "    Cb: $($candidate.cb), Displacement: $($candidate.dispT) t" -ForegroundColor White
            Write-Host "    Geometry Status: $($candidate.geometryGenerationStatus)" -ForegroundColor $(if ($candidate.geometryGenerationStatus -eq "Success") { "Green" } else { "Yellow" })

            # Check geometry sources
            $hasGeometryJson = ![string]::IsNullOrEmpty($candidate.geometryJson)
            $hasShipDParams = ![string]::IsNullOrEmpty($candidate.shipdParametersJson)

            Write-Host "    Has geometryJson: $hasGeometryJson" -ForegroundColor $(if ($hasGeometryJson) { "Green" } else { "Yellow" })
            Write-Host "    Has shipdParametersJson: $hasShipDParams" -ForegroundColor $(if ($hasShipDParams) { "Green" } else { "Yellow" })

            # Analyze ShipD parameters if available
            if ($hasShipDParams) {
                try {
                    $vector = $candidate.shipdParametersJson | ConvertFrom-Json
                    $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
                    Write-Host "    ShipD Vector: $nonZeroCount/45 non-zero parameters" -ForegroundColor $(if ($nonZeroCount -gt 5) { "Green" } else { "Yellow" })
                    Write-Host "    Vector[1] (Bow Ratio): $($vector[1])" -ForegroundColor $(if ($vector[1] -gt 0) { "Green" } else { "Red" })
                    Write-Host "    Vector[2] (Stern Ratio): $($vector[2])" -ForegroundColor $(if ($vector[2] -gt 0) { "Green" } else { "Red" })
                    Write-Host "    Vector[20] (Service Speed): $($vector[20])" -ForegroundColor $(if ($vector[20] -gt 0) { "Green" } else { "Red" })
                } catch {
                    Write-Host "    ⚠️ Failed to parse ShipD vector" -ForegroundColor Yellow
                }
            }

            # Store results
            $results += [PSCustomObject]@{
                VesselType = $vessel.Name
                Rank = $rank
                LppM = $candidate.lppM
                BeamM = $candidate.beamM
                DraftM = $candidate.draftM
                Cb = $candidate.cb
                DisplacementT = $candidate.dispT
                GeometryStatus = $candidate.geometryGenerationStatus
                HasGeometryJson = $hasGeometryJson
                HasShipDParams = $hasShipDParams
                CandidateId = $candidate.id
            }
        }

        # Compare candidates 1-2 vs 3-5
        Write-Host "`n  Comparison Analysis:" -ForegroundColor Cyan
        $candidates12 = $candidates[0..1]
        $candidates35 = $candidates[2..4]

        $avgCb12 = ($candidates12 | Measure-Object -Property cb -Average).Average
        $avgCb35 = ($candidates35 | Measure-Object -Property cb -Average).Average

        Write-Host "    Avg Cb (1-2): $([math]::Round($avgCb12, 4))" -ForegroundColor White
        Write-Host "    Avg Cb (3-5): $([math]::Round($avgCb35, 4))" -ForegroundColor White
        Write-Host "    Cb Difference: $([math]::Round($avgCb35 - $avgCb12, 4))" -ForegroundColor $(if ([math]::Abs($avgCb35 - $avgCb12) -gt 0.01) { "Green" } else { "Yellow" })

    } catch {
        Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Response: $responseBody" -ForegroundColor Red
        }
    }
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Total candidates analyzed: $($results.Count)" -ForegroundColor White
Write-Host "Candidates with geometry: $(($results | Where-Object { $_.HasGeometryJson }).Count)" -ForegroundColor White
Write-Host "Candidates with ShipD params: $(($results | Where-Object { $_.HasShipDParams }).Count)" -ForegroundColor White

# Export results
$results | Export-Csv -Path "temp/geometry-investigation-results.csv" -NoTypeInformation
Write-Host "`nResults exported to: temp/geometry-investigation-results.csv" -ForegroundColor Green
