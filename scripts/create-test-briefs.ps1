# Script to create test briefs via API
# This creates two briefs: Yacht with fine families and Cargo with full families

$baseUrl = "http://localhost:5002/api/v1/hull-sizing"
$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "Creating test briefs via API..." -ForegroundColor Cyan

# Function to create a mission case
function Create-MissionCase {
    param(
        [string]$Name,
        [string]$MissionType,
        [string]$MissionCategory,
        [string]$CargoBasis,
        [decimal]$CargoValue,
        [decimal]$ServiceSpeedKn,
        [string]$BowFamily,
        [string]$MidshipFamily,
        [string]$SternFamily
    )

    $body = @{
        name = $Name
        missionType = $MissionType
        missionCategory = $MissionCategory
        cargoBasis = $CargoBasis
        cargoValue = $CargoValue
        serviceSpeedKn = $ServiceSpeedKn
        seaMarginPct = 15.0
        bowFamily = $BowFamily
        midshipFamily = $MidshipFamily
        sternFamily = $SternFamily
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/mission-cases" -Method POST -Headers $headers -Body $body
        Write-Host "✅ Created mission case: $Name (ID: $($response.id))" -ForegroundColor Green
        return $response
    }
    catch {
        Write-Host "❌ Failed to create mission case: $Name" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
        return $null
    }
}

# Function to create a sizing run
function Create-SizingRun {
    param(
        [string]$MissionCaseId,
        [string]$VesselType,
        [string]$BowFamily,
        [string]$MidshipFamily,
        [string]$SternFamily
    )

    $body = @{
        missionCaseId = $MissionCaseId
        mode = "first_principles"
        vesselType = $VesselType
        bowFamily = $BowFamily
        midshipFamily = $MidshipFamily
        sternFamily = $SternFamily
        options = @{
            maxCandidates = 5
            minFn = 0.15
            maxFn = 0.35
            includeGeometry = $true
        }
    } | ConvertTo-Json -Depth 10

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/runs" -Method POST -Headers $headers -Body $body
        Write-Host "✅ Created sizing run for mission case $MissionCaseId (ID: $($response.id))" -ForegroundColor Green
        return $response
    }
    catch {
        Write-Host "❌ Failed to create sizing run for mission case $MissionCaseId" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
        return $null
    }
}

# Create Yacht Brief
Write-Host "`n📝 Creating Yacht Brief (Fine Families)..." -ForegroundColor Yellow
$yachtMission = Create-MissionCase `
    -Name "Yacht Test - Fine Families (v2)" `
    -MissionType "yacht" `
    -MissionCategory "Recreational" `
    -CargoBasis "Weight" `
    -CargoValue 500 `
    -ServiceSpeedKn 20 `
    -BowFamily "fine_entry" `
    -MidshipFamily "deep_v_midship" `
    -SternFamily "canoe_stern"

if ($yachtMission) {
    Write-Host "⏳ Creating sizing run for Yacht brief..." -ForegroundColor Yellow
    Start-Sleep -Seconds 1
    $yachtRun = Create-SizingRun `
        -MissionCaseId $yachtMission.id `
        -VesselType "yacht" `
        -BowFamily "fine_entry" `
        -MidshipFamily "deep_v_midship" `
        -SternFamily "canoe_stern"
}

# Create Cargo Brief
Write-Host "`n📝 Creating Cargo Brief (Full Families)..." -ForegroundColor Yellow
$cargoMission = Create-MissionCase `
    -Name "Cargo Test - Full Families (v2)" `
    -MissionType "general_cargo" `
    -MissionCategory "Commercial" `
    -CargoBasis "Weight" `
    -CargoValue 10000 `
    -ServiceSpeedKn 15 `
    -BowFamily "bulbous_bow" `
    -MidshipFamily "full_midship" `
    -SternFamily "transom_stern"

if ($cargoMission) {
    Write-Host "⏳ Creating sizing run for Cargo brief..." -ForegroundColor Yellow
    Start-Sleep -Seconds 1
    $cargoRun = Create-SizingRun `
        -MissionCaseId $cargoMission.id `
        -VesselType "general_cargo" `
        -BowFamily "bulbous_bow" `
        -MidshipFamily "full_midship" `
        -SternFamily "transom_stern"
}

Write-Host "`n✅ Test briefs creation complete!" -ForegroundColor Green
Write-Host "`nYou can now:" -ForegroundColor Cyan
if ($yachtRun) {
    Write-Host "  - View Yacht brief: http://localhost:3000/sizing/runs/$($yachtRun.id)" -ForegroundColor White
}
if ($cargoRun) {
    Write-Host "  - View Cargo brief: http://localhost:3000/sizing/runs/$($cargoRun.id)" -ForegroundColor White
}
