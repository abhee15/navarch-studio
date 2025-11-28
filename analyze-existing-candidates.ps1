# Analyze existing candidates from completed run
$baseUrl = "http://localhost:5004/api/v1"
$runId = "a7a5af35-ab9a-4741-8155-3dc3291821b9"

Write-Host "=== Analyzing Candidates from Run: $runId ===" -ForegroundColor Cyan

try {
    $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$runId/candidates" -Method GET
    Write-Host "Found $($candidates.Count) candidates`n" -ForegroundColor Green

    for ($i = 0; $i -lt $candidates.Count; $i++) {
        $c = $candidates[$i]
        $rank = $i + 1

        Write-Host "=== Candidate $rank ===" -ForegroundColor Yellow
        Write-Host "  Dimensions:" -ForegroundColor Cyan
        Write-Host "    Lpp: $($c.lppM) m" -ForegroundColor White
        Write-Host "    Beam: $($c.beamM) m" -ForegroundColor White
        Write-Host "    Draft: $($c.draftM) m" -ForegroundColor White
        Write-Host "    Depth: $($c.depthM) m" -ForegroundColor White
        Write-Host "  Coefficients:" -ForegroundColor Cyan
        Write-Host "    Cb: $($c.cb)" -ForegroundColor White
        Write-Host "    Cp: $($c.cp)" -ForegroundColor White
        Write-Host "    Cwp: $($c.cwp)" -ForegroundColor White
        Write-Host "    Cm: $($c.cm)" -ForegroundColor White
        Write-Host "  Performance:" -ForegroundColor Cyan
        Write-Host "    Displacement: $($c.dispT) t" -ForegroundColor White
        Write-Host "    Score: $($c.score)" -ForegroundColor White
        Write-Host "  Geometry:" -ForegroundColor Cyan
        Write-Host "    Status: $($c.geometryGenerationStatus)" -ForegroundColor $(if ($c.geometryGenerationStatus -eq "Success") { "Green" } else { "Yellow" })
        $hasGeometry = ![string]::IsNullOrEmpty($c.geometryJson)
        $hasShipD = ![string]::IsNullOrEmpty($c.shipdParametersJson)
        Write-Host "    Has geometryJson: $hasGeometry" -ForegroundColor $(if ($hasGeometry) { "Green" } else { "Yellow" })
        Write-Host "    Has shipdParametersJson: $hasShipD" -ForegroundColor $(if ($hasShipD) { "Green" } else { "Yellow" })

        if ($c.geometryGenerationError) {
            Write-Host "    Error: $($c.geometryGenerationError)" -ForegroundColor Red
        }

        # Analyze ShipD parameters
        if ($hasShipD) {
            try {
                $vector = $c.shipdParametersJson | ConvertFrom-Json
                $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
                Write-Host "  ShipD Vector Analysis:" -ForegroundColor Cyan
                Write-Host "    Non-zero parameters: $nonZeroCount/45" -ForegroundColor $(if ($nonZeroCount -gt 5) { "Green" } else { "Yellow" })
                Write-Host "    Vector[1] (Bow Ratio): $($vector[1])" -ForegroundColor $(if ($vector[1] -gt 0) { "Green" } else { "Red" })
                Write-Host "    Vector[2] (Stern Ratio): $($vector[2])" -ForegroundColor $(if ($vector[2] -gt 0) { "Green" } else { "Red" })
                Write-Host "    Vector[20] (Service Speed): $($vector[20])" -ForegroundColor $(if ($vector[20] -gt 0) { "Green" } else { "Red" })
                Write-Host "    Vector[31] (bit_BB): $($vector[31])" -ForegroundColor $(if ($vector[31] -gt 0) { "Green" } else { "Gray" })

                # Check if vector is mostly zeros
                if ($nonZeroCount -le 3) {
                    Write-Host "    ⚠️ WARNING: ShipD vector is mostly zeros!" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "    ⚠️ Failed to parse ShipD vector: $_" -ForegroundColor Yellow
            }
        }

        Write-Host ""
    }

    # Compare candidates 1-2 vs 3-5
    Write-Host "=== Comparison: Candidates 1-2 vs 3-5 ===" -ForegroundColor Cyan
    $group1 = $candidates[0..1]
    $group2 = $candidates[2..4]

    $avgCb1 = ($group1 | Measure-Object -Property cb -Average).Average
    $avgCb2 = ($group2 | Measure-Object -Property cb -Average).Average
    $avgLpp1 = ($group1 | Measure-Object -Property lppM -Average).Average
    $avgLpp2 = ($group2 | Measure-Object -Property lppM -Average).Average
    $avgBeam1 = ($group1 | Measure-Object -Property beamM -Average).Average
    $avgBeam2 = ($group2 | Measure-Object -Property beamM -Average).Average

    Write-Host "Group 1 (Candidates 1-2):" -ForegroundColor Yellow
    Write-Host "  Avg Cb: $([math]::Round($avgCb1, 4))" -ForegroundColor White
    Write-Host "  Avg Lpp: $([math]::Round($avgLpp1, 2)) m" -ForegroundColor White
    Write-Host "  Avg Beam: $([math]::Round($avgBeam1, 2)) m" -ForegroundColor White

    Write-Host "Group 2 (Candidates 3-5):" -ForegroundColor Yellow
    Write-Host "  Avg Cb: $([math]::Round($avgCb2, 4))" -ForegroundColor White
    Write-Host "  Avg Lpp: $([math]::Round($avgLpp2, 2)) m" -ForegroundColor White
    Write-Host "  Avg Beam: $([math]::Round($avgBeam2, 2)) m" -ForegroundColor White

    Write-Host "Differences:" -ForegroundColor Yellow
    Write-Host "  Cb Diff: $([math]::Round($avgCb2 - $avgCb1, 4)) ($([math]::Round((($avgCb2 - $avgCb1) / $avgCb1 * 100), 1))%)" -ForegroundColor White
    Write-Host "  Lpp Diff: $([math]::Round($avgLpp2 - $avgLpp1, 2)) m ($([math]::Round((($avgLpp2 - $avgLpp1) / $avgLpp1 * 100), 1))%)" -ForegroundColor White
    Write-Host "  Beam Diff: $([math]::Round($avgBeam2 - $avgBeam1, 2)) m ($([math]::Round((($avgBeam2 - $avgBeam1) / $avgBeam1 * 100), 1))%)" -ForegroundColor White

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}
