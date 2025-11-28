# Comprehensive Geometry Testing Script
# Tests multiple vessel types, verifies ShipD vectors, and documents results

$baseUrl = "http://localhost:5004/api/v1"
$resultsDir = "temp/geometry-test-results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

Write-Host "`n=== Comprehensive Hull Sizing Geometry Testing ===" -ForegroundColor Cyan
Write-Host "Testing multiple vessel types with detailed verification`n" -ForegroundColor Yellow

# Test vessel configurations
$vesselConfigs = @(
    @{
        Name = "500 TEU Container Feeder"
        Category = "commercial"
        Type = "container"
        CargoBasis = "teu"
        CargoValue = 500
        TeuCount = 500
        ServiceSpeedKn = 18.0
        BowFamily = "bulbous"
        MidshipFamily = "u"
        SternFamily = "transom"
    },
    @{
        Name = "10K DWT General Cargo"
        Category = "commercial"
        Type = "general_cargo"
        CargoBasis = "weight"
        CargoValue = 10000
        ServiceSpeedKn = 15.0
        BowFamily = "straight"
        MidshipFamily = "u"
        SternFamily = "transom"
    },
    @{
        Name = "50K DWT Bulk Carrier"
        Category = "commercial"
        Type = "bulk"
        CargoBasis = "weight"
        CargoValue = 50000
        ServiceSpeedKn = 14.0
        BowFamily = "straight"
        MidshipFamily = "u"
        SternFamily = "transom"
    }
)

$allResults = @()

foreach ($vessel in $vesselConfigs) {
    Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
    Write-Host "Testing: $($vessel.Name)" -ForegroundColor Green
    Write-Host ("=" * 80) -ForegroundColor Cyan

    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $missionCaseName = "Test - $($vessel.Name) $timestamp"

    # Create mission case
    $missionCaseDto = @{
        name = $missionCaseName
        missionCategory = $vessel.Category
        missionType = $vessel.Type
        cargoBasis = $vessel.CargoBasis
        cargoValue = $vessel.CargoValue
        serviceSpeedKn = $vessel.ServiceSpeedKn
        bowFamily = $vessel.BowFamily
        midshipFamily = $vessel.MidshipFamily
        sternFamily = $vessel.SternFamily
    }

    if ($vessel.ContainsKey("TeuCount")) {
        $missionCaseDto["teuCount"] = $vessel.TeuCount
    }

    try {
        Write-Host "`n[1] Creating mission case..." -ForegroundColor Yellow
        $missionCase = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/mission-cases" -Method POST -ContentType "application/json" -Body ($missionCaseDto | ConvertTo-Json -Depth 10)
        Write-Host "  ✅ Mission Case ID: $($missionCase.id)" -ForegroundColor Green

        # Create sizing run
        $sizingRunDto = @{
            MissionCaseId = $missionCase.id
            VesselCategory = $vessel.Type
            VesselType = $vessel.Name
            BowFamily = $vessel.BowFamily
            MidshipFamily = $vessel.MidshipFamily
            SternFamily = $vessel.SternFamily
            SolverMode = "parametric"
        }

        Write-Host "`n[2] Creating sizing run..." -ForegroundColor Yellow
        $run = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs" -Method POST -ContentType "application/json" -Body ($sizingRunDto | ConvertTo-Json -Depth 10)
        Write-Host "  ✅ Run ID: $($run.id)" -ForegroundColor Green

        # Wait for completion
        Write-Host "`n[3] Waiting for run completion..." -ForegroundColor Yellow
        $maxWait = 60
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

        Write-Host "  ✅ Run completed in $($run.computeTimeMs)ms" -ForegroundColor Green

        # Fetch candidates
        Write-Host "`n[4] Fetching candidates..." -ForegroundColor Yellow
        $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$($run.id)/candidates" -Method GET
        Write-Host "  ✅ Found $($candidates.Count) candidates" -ForegroundColor Green

        # Detailed analysis of each candidate
        Write-Host "`n[5] Analyzing candidates..." -ForegroundColor Yellow
        $vesselResults = @()

        for ($i = 0; $i -lt $candidates.Count; $i++) {
            $c = $candidates[$i]
            $rank = $i + 1

            Write-Host "`n  Candidate ${rank}:" -ForegroundColor Cyan
            Write-Host "    Dimensions: Lpp=$($c.lppM)m, Beam=$($c.beamM)m, Draft=$($c.draftM)m" -ForegroundColor White
            Write-Host "    Coefficients: Cb=$($c.cb), Cp=$($c.cp), Cwp=$($c.cwp), Cm=$($c.cm)" -ForegroundColor White
            Write-Host "    Displacement: $($c.dispT)t, Score: $($c.score)" -ForegroundColor White

            # ShipD Vector Analysis
            $shipdAnalysis = @{
                HasVector = $false
                NonZeroCount = 0
                Vector1 = 0
                Vector2 = 0
                Vector20 = 0
                Vector31 = 0
                IsValid = $false
            }

            if (![string]::IsNullOrEmpty($c.shipdParametersJson)) {
                try {
                    $vector = $c.shipdParametersJson | ConvertFrom-Json
                    $shipdAnalysis.HasVector = $true
                    $shipdAnalysis.NonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
                    $shipdAnalysis.Vector1 = $vector[1]
                    $shipdAnalysis.Vector2 = $vector[2]
                    $shipdAnalysis.Vector20 = $vector[20]
                    $shipdAnalysis.Vector31 = $vector[31]
                    $shipdAnalysis.IsValid = $shipdAnalysis.Vector1 -gt 0 -and $shipdAnalysis.Vector2 -gt 0 -and $shipdAnalysis.NonZeroCount -gt 3

                    Write-Host "    ShipD Vector:" -ForegroundColor $(if ($shipdAnalysis.IsValid) { "Green" } else { "Yellow" })
                    Write-Host "      Non-zero params: $($shipdAnalysis.NonZeroCount)/45" -ForegroundColor White
                    Write-Host "      Vector[1] (Bow): $($shipdAnalysis.Vector1)" -ForegroundColor $(if ($shipdAnalysis.Vector1 -gt 0) { "Green" } else { "Red" })
                    Write-Host "      Vector[2] (Stern): $($shipdAnalysis.Vector2)" -ForegroundColor $(if ($shipdAnalysis.Vector2 -gt 0) { "Green" } else { "Red" })
                    Write-Host "      Vector[20] (Speed): $($shipdAnalysis.Vector20)" -ForegroundColor White
                    Write-Host "      Vector[31] (bit_BB): $($shipdAnalysis.Vector31)" -ForegroundColor White

                    if (-not $shipdAnalysis.IsValid) {
                        Write-Host "      ⚠️ WARNING: ShipD vector is invalid or mostly zeros!" -ForegroundColor Yellow
                    } else {
                        Write-Host "      ✅ ShipD vector is valid" -ForegroundColor Green
                    }
                } catch {
                    Write-Host "    ⚠️ Failed to parse ShipD vector: $_" -ForegroundColor Yellow
                }
            } else {
                Write-Host "    ❌ No ShipD parameters" -ForegroundColor Red
            }

            # Geometry Analysis
            $hasGeometry = ![string]::IsNullOrEmpty($c.geometryJson)
            $geometryStatus = $c.geometryGenerationStatus

            Write-Host "    Geometry:" -ForegroundColor $(if ($geometryStatus -eq "Success") { "Green" } else { "Yellow" })
            Write-Host "      Status: $geometryStatus" -ForegroundColor White
            Write-Host "      Has geometryJson: $hasGeometry" -ForegroundColor $(if ($hasGeometry) { "Green" } else { "Yellow" })

            if ($c.geometryGenerationError) {
                Write-Host "      Error: $($c.geometryGenerationError)" -ForegroundColor Red
            }

            # Store results
            $vesselResults += [PSCustomObject]@{
                VesselType = $vessel.Name
                Rank = $rank
                CandidateId = $c.id
                LppM = $c.lppM
                BeamM = $c.beamM
                DraftM = $c.draftM
                Cb = $c.cb
                Cp = $c.cp
                Cwp = $c.cwp
                Cm = $c.cm
                DisplacementT = $c.dispT
                Score = $c.score
                GeometryStatus = $geometryStatus
                HasGeometry = $hasGeometry
                ShipdVectorValid = $shipdAnalysis.IsValid
                ShipdVector1 = $shipdAnalysis.Vector1
                ShipdVector2 = $shipdAnalysis.Vector2
                ShipdNonZeroCount = $shipdAnalysis.NonZeroCount
                RunId = $run.id
            }
        }

        # Compare candidates
        Write-Host "`n[6] Comparing candidates..." -ForegroundColor Yellow
        $group1 = $vesselResults[0..1]
        $group2 = $vesselResults[2..4]

        $avgCb1 = ($group1 | Measure-Object -Property Cb -Average).Average
        $avgCb2 = ($group2 | Measure-Object -Property Cb -Average).Average
        $avgLpp1 = ($group1 | Measure-Object -Property LppM -Average).Average
        $avgLpp2 = ($group2 | Measure-Object -Property LppM -Average).Average

        Write-Host "  Group 1 (Candidates 1-2):" -ForegroundColor Cyan
        Write-Host "    Avg Cb: $([math]::Round($avgCb1, 4))" -ForegroundColor White
        Write-Host "    Avg Lpp: $([math]::Round($avgLpp1, 2))m" -ForegroundColor White

        Write-Host "  Group 2 (Candidates 3-5):" -ForegroundColor Cyan
        Write-Host "    Avg Cb: $([math]::Round($avgCb2, 4))" -ForegroundColor White
        Write-Host "    Avg Lpp: $([math]::Round($avgLpp2, 2))m" -ForegroundColor White

        Write-Host "  Differences:" -ForegroundColor Cyan
        Write-Host "    Cb Diff: $([math]::Round($avgCb2 - $avgCb1, 4)) ($([math]::Round((($avgCb2 - $avgCb1) / $avgCb1 * 100), 1))%)" -ForegroundColor White
        Write-Host "    Lpp Diff: $([math]::Round($avgLpp2 - $avgLpp1, 2))m ($([math]::Round((($avgLpp2 - $avgLpp1) / $avgLpp1 * 100), 1))%)" -ForegroundColor White

        # Check ShipD vector validity
        $validVectors = ($vesselResults | Where-Object { $_.ShipdVectorValid }).Count
        Write-Host "`n  ShipD Vector Status:" -ForegroundColor Cyan
        Write-Host "    Valid vectors: $validVectors/5" -ForegroundColor $(if ($validVectors -eq 5) { "Green" } else { "Yellow" })

        # Export results
        $vesselResults | Export-Csv -Path "$resultsDir/$($vessel.Name.Replace(' ', '_'))_results.csv" -NoTypeInformation
        $allResults += $vesselResults

        Write-Host "`n  ✅ Results exported to: $resultsDir/$($vessel.Name.Replace(' ', '_'))_results.csv" -ForegroundColor Green

    } catch {
        Write-Host "`n  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Response: $responseBody" -ForegroundColor Red
        }
    }
}

# Summary
Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Cyan

Write-Host "`nTotal candidates tested: $($allResults.Count)" -ForegroundColor White
Write-Host "Candidates with valid ShipD vectors: $(($allResults | Where-Object { $_.ShipdVectorValid }).Count)" -ForegroundColor $(if (($allResults | Where-Object { $_.ShipdVectorValid }).Count -eq $allResults.Count) { "Green" } else { "Yellow" })
Write-Host "Candidates with geometry: $(($allResults | Where-Object { $_.HasGeometry }).Count)" -ForegroundColor White

# Export all results
$allResults | Export-Csv -Path "$resultsDir/all_results.csv" -NoTypeInformation
Write-Host "`n✅ All results exported to: $resultsDir/all_results.csv" -ForegroundColor Green

Write-Host "`n=== Testing Complete ===" -ForegroundColor Cyan
