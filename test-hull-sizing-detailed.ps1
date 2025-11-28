# Detailed Hull Sizing Investigation
Write-Host "=== Detailed Hull Sizing Investigation ===" -ForegroundColor Cyan
Write-Host "Testing: 500 TEU Container Feeder" -ForegroundColor Yellow

# Step 1: Get or Create Mission Case for 500 TEU Container Feeder
Write-Host "`n[1] Getting Mission Case (500 TEU Container Feeder)..." -ForegroundColor Yellow
try {
    # Try to get existing mission cases
    $existingCases = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/mission-cases" `
        -Method GET

    $missionCase = $existingCases | Where-Object { $_.name -eq "500 TEU Container Feeder" } | Select-Object -First 1

    if ($missionCase) {
        $missionCaseId = $missionCase.id
        Write-Host "✅ Found Existing Mission Case: $missionCaseId" -ForegroundColor Green
    }
    else {
        # Create new one
        Write-Host "Creating new mission case..." -ForegroundColor Gray
        $missionCaseJson = @{
            name               = "500 TEU Container Feeder Test $(Get-Date -Format 'HHmmss')"
            missionCategory    = "commercial"
            missionType        = "container"
            cargoBasis         = "teu"
            teuCount           = 500
            cargoValue         = 500
            cargoDensityTPerM3 = 0.5
            serviceSpeedKn     = 18.0
            seaMarginPct       = 15.0
            bowFamily          = "bulbous"
            midshipFamily      = "u"
            sternFamily        = "transom"
        } | ConvertTo-Json

        $missionCaseResponse = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/mission-cases" `
            -Method POST `
            -Body $missionCaseJson `
            -ContentType "application/json"

        $missionCaseId = $missionCaseResponse.id
        Write-Host "✅ Mission Case Created: $missionCaseId" -ForegroundColor Green
    }
}
catch {
    Write-Host "❌ Failed to get/create mission case: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
    exit 1
}

# Step 2: Create First Sizing Run
Write-Host "`n[2] Creating First Sizing Run..." -ForegroundColor Yellow
$sizingRunJson = @{
    missionCaseId = $missionCaseId
    mode          = "first_principles"
    options       = @{
        maxCandidates   = 5
        includeGeometry = $true
    }
} | ConvertTo-Json -Depth 3

try {
    $sizingRun1 = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs" `
        -Method POST `
        -Body $sizingRunJson `
        -ContentType "application/json"

    $sizingRunId1 = $sizingRun1.id
    Write-Host "✅ First Sizing Run Created: $sizingRunId1" -ForegroundColor Green
    Write-Host "   Status: $($sizingRun1.runStatus)" -ForegroundColor Gray
    Write-Host "   Candidate Count: $($sizingRun1.candidateCount)" -ForegroundColor Gray
    Write-Host "   Compute Time: $($sizingRun1.computeTimeMs)ms" -ForegroundColor Gray

    # Wait for completion
    Start-Sleep -Seconds 2

    # Get candidates from first run
    Write-Host "`n[3] Fetching Candidates from First Run..." -ForegroundColor Yellow
    $candidates1 = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs/$sizingRunId1/candidates" `
        -Method GET

    Write-Host "✅ Found $($candidates1.Count) candidates" -ForegroundColor Green

    # Display all candidates from first run
    Write-Host "`n=== FIRST RUN CANDIDATES ===" -ForegroundColor Cyan
    for ($i = 0; $i -lt $candidates1.Count; $i++) {
        $candidate = $candidates1[$i]
        Write-Host "`nCandidate $($i + 1):" -ForegroundColor Yellow
        Write-Host "  ID: $($candidate.id)" -ForegroundColor Gray
        Write-Host "  LPP: $($candidate.lppM)m" -ForegroundColor Gray
        Write-Host "  Beam: $($candidate.beamM)m" -ForegroundColor Gray
        Write-Host "  Draft: $($candidate.draftM)m" -ForegroundColor Gray
        Write-Host "  Displacement: $($candidate.dispT)t" -ForegroundColor Gray
        Write-Host "  Block Coefficient: $($candidate.cb)" -ForegroundColor Gray
        Write-Host "  Score: $($candidate.score)" -ForegroundColor Gray
        Write-Host "  Rank: $($candidate.rank)" -ForegroundColor Gray
        if ($candidate.bowFamily) { Write-Host "  Bow: $($candidate.bowFamily)" -ForegroundColor Gray }
        if ($candidate.midshipFamily) { Write-Host "  Midship: $($candidate.midshipFamily)" -ForegroundColor Gray }
        if ($candidate.sternFamily) { Write-Host "  Stern: $($candidate.sternFamily)" -ForegroundColor Gray }
    }

    # Compare designs 1-2 vs 3-5
    Write-Host "`n=== COMPARISON: Designs 1-2 vs 3-5 ===" -ForegroundColor Cyan
    $group1 = $candidates1[0..1]
    $group2 = $candidates1[2..4]

    Write-Host "`nGroup 1 (Designs 1-2) Average:" -ForegroundColor Yellow
    $avgLpp1 = ($group1 | Measure-Object -Property lppM -Average).Average
    $avgBeam1 = ($group1 | Measure-Object -Property beamM -Average).Average
    $avgDraft1 = ($group1 | Measure-Object -Property draftM -Average).Average
    $avgDisp1 = ($group1 | Measure-Object -Property dispT -Average).Average
    $avgCb1 = ($group1 | Measure-Object -Property cb -Average).Average
    Write-Host "  Avg LPP: $([math]::Round($avgLpp1, 2))m" -ForegroundColor Gray
    Write-Host "  Avg Beam: $([math]::Round($avgBeam1, 2))m" -ForegroundColor Gray
    Write-Host "  Avg Draft: $([math]::Round($avgDraft1, 2))m" -ForegroundColor Gray
    Write-Host "  Avg Displacement: $([math]::Round($avgDisp1, 2))t" -ForegroundColor Gray
    Write-Host "  Avg Cb: $([math]::Round($avgCb1, 3))" -ForegroundColor Gray

    Write-Host "`nGroup 2 (Designs 3-5) Average:" -ForegroundColor Yellow
    $avgLpp2 = ($group2 | Measure-Object -Property lppM -Average).Average
    $avgBeam2 = ($group2 | Measure-Object -Property beamM -Average).Average
    $avgDraft2 = ($group2 | Measure-Object -Property draftM -Average).Average
    $avgDisp2 = ($group2 | Measure-Object -Property dispT -Average).Average
    $avgCb2 = ($group2 | Measure-Object -Property cb -Average).Average
    Write-Host "  Avg LPP: $([math]::Round($avgLpp2, 2))m" -ForegroundColor Gray
    Write-Host "  Avg Beam: $([math]::Round($avgBeam2, 2))m" -ForegroundColor Gray
    Write-Host "  Avg Draft: $([math]::Round($avgDraft2, 2))m" -ForegroundColor Gray
    Write-Host "  Avg Displacement: $([math]::Round($avgDisp2, 2))t" -ForegroundColor Gray
    Write-Host "  Avg Cb: $([math]::Round($avgCb2, 3))" -ForegroundColor Gray

    Write-Host "`nDifferences:" -ForegroundColor Yellow
    Write-Host "  LPP Diff: $([math]::Round($avgLpp1 - $avgLpp2, 2))m ($([math]::Round((($avgLpp1 - $avgLpp2) / $avgLpp2 * 100), 1))%)" -ForegroundColor Gray
    Write-Host "  Beam Diff: $([math]::Round($avgBeam1 - $avgBeam2, 2))m ($([math]::Round((($avgBeam1 - $avgBeam2) / $avgBeam2 * 100), 1))%)" -ForegroundColor Gray
    Write-Host "  Draft Diff: $([math]::Round($avgDraft1 - $avgDraft2, 2))m ($([math]::Round((($avgDraft1 - $avgDraft2) / $avgDraft2 * 100), 1))%)" -ForegroundColor Gray
    Write-Host "  Displacement Diff: $([math]::Round($avgDisp1 - $avgDisp2, 2))t ($([math]::Round((($avgDisp1 - $avgDisp2) / $avgDisp2 * 100), 1))%)" -ForegroundColor Gray
    Write-Host "  Cb Diff: $([math]::Round($avgCb1 - $avgCb2, 3)) ($([math]::Round((($avgCb1 - $avgCb2) / $avgCb2 * 100), 1))%)" -ForegroundColor Gray

}
catch {
    Write-Host "❌ Error in first run: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Create Second Sizing Run (rerun)
Write-Host "`n[4] Creating Second Sizing Run (Rerun)..." -ForegroundColor Yellow
try {
    $sizingRun2 = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs" `
        -Method POST `
        -Body $sizingRunJson `
        -ContentType "application/json"

    $sizingRunId2 = $sizingRun2.id
    Write-Host "✅ Second Sizing Run Created: $sizingRunId2" -ForegroundColor Green
    Write-Host "   Status: $($sizingRun2.runStatus)" -ForegroundColor Gray
    Write-Host "   Candidate Count: $($sizingRun2.candidateCount)" -ForegroundColor Gray
    Write-Host "   Compute Time: $($sizingRun2.computeTimeMs)ms" -ForegroundColor Gray

    Start-Sleep -Seconds 2

    # Get candidates from second run
    Write-Host "`n[5] Fetching Candidates from Second Run..." -ForegroundColor Yellow
    $candidates2 = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs/$sizingRunId2/candidates" `
        -Method GET

    Write-Host "✅ Found $($candidates2.Count) candidates" -ForegroundColor Green

    # Compare first run vs second run
    Write-Host "`n=== COMPARISON: First Run vs Second Run ===" -ForegroundColor Cyan
    Write-Host "`nFirst Run Candidates:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $candidates1.Count; $i++) {
        $c = $candidates1[$i]
        Write-Host "  $($i + 1). LPP: $($c.lppM)m, Beam: $($c.beamM)m, Draft: $($c.draftM)m, Disp: $($c.dispT)t, Cb: $($c.cb), Score: $($c.score), Rank: $($c.rank)" -ForegroundColor Gray
    }

    Write-Host "`nSecond Run Candidates:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $candidates2.Count; $i++) {
        $c = $candidates2[$i]
        Write-Host "  $($i + 1). LPP: $($c.lppM)m, Beam: $($c.beamM)m, Draft: $($c.draftM)m, Disp: $($c.dispT)t, Cb: $($c.cb), Score: $($c.score), Rank: $($c.rank)" -ForegroundColor Gray
    }

    # Check if results changed
    $changed = $false
    for ($i = 0; $i -lt [Math]::Min($candidates1.Count, $candidates2.Count); $i++) {
        $c1 = $candidates1[$i]
        $c2 = $candidates2[$i]
        if ($c1.lppM -ne $c2.lppM -or $c1.beamM -ne $c2.beamM -or $c1.draftM -ne $c2.draftM) {
            $changed = $true
            Write-Host "`n⚠️  Candidate $($i + 1) CHANGED between runs!" -ForegroundColor Yellow
            Write-Host "  First:  LPP=$($c1.lppM)m, Beam=$($c1.beamM)m, Draft=$($c1.draftM)m" -ForegroundColor Gray
            Write-Host "  Second: LPP=$($c2.lppM)m, Beam=$($c2.beamM)m, Draft=$($c2.draftM)m" -ForegroundColor Gray
        }
    }

    if (-not $changed) {
        Write-Host "`n✅ Results are consistent between runs" -ForegroundColor Green
    }

}
catch {
    Write-Host "❌ Error in second run: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Check if vessels were created and get details
Write-Host "`n[6] Checking if Vessels Were Created..." -ForegroundColor Yellow
Write-Host "`nChecking candidates for vessel references..." -ForegroundColor Gray
for ($i = 0; $i -lt $candidates1.Count; $i++) {
    $c = $candidates1[$i]
    Write-Host "`nCandidate $($i + 1):" -ForegroundColor Yellow
    Write-Host "  Has Geometry: $($c.hasValidGeometry)" -ForegroundColor Gray
    Write-Host "  Geometry Status: $($c.geometryGenerationStatus)" -ForegroundColor Gray
    if ($c.geometryGenerationError) {
        Write-Host "  Geometry Error: $($c.geometryGenerationError)" -ForegroundColor Red
    }
    Write-Host "  ShipD Parameters: $($c.shipdParametersJson)" -ForegroundColor Gray
    Write-Host "  Solver Mode: $($c.solverMode)" -ForegroundColor Gray
    if ($c.referenceVesselId) {
        Write-Host "  Reference Vessel: $($c.referenceVesselName) ($($c.referenceVesselId))" -ForegroundColor Gray
        Write-Host "  Similarity Score: $($c.similarityScore)" -ForegroundColor Gray
    }
}

Write-Host "`n=== Investigation Complete ===" -ForegroundColor Cyan
Write-Host "Mission Case ID: $missionCaseId" -ForegroundColor Gray
Write-Host "First Run ID: $sizingRunId1" -ForegroundColor Gray
if ($sizingRunId2) { Write-Host "Second Run ID: $sizingRunId2" -ForegroundColor Gray }
