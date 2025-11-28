# Quick test to verify taxonomy defaults are applied
$missionCaseId = "ec6590c2-1b29-42c7-98d9-6580b878adf0"
$sizingRunJson = @{
    missionCaseId = $missionCaseId
    mode          = "first_principles"
    options       = @{
        maxCandidates   = 1
        includeGeometry = $true
    }
} | ConvertTo-Json -Depth 3

Write-Host "Creating sizing run..." -ForegroundColor Yellow
$run = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs" `
    -Method POST `
    -Body $sizingRunJson `
    -ContentType "application/json"

Start-Sleep -Seconds 3

$candidates = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/hull-sizing/runs/$($run.id)/candidates"

Write-Host "`n=== ShipD Parameters Check ===" -ForegroundColor Cyan
$vector = $candidates[0].shipdParametersJson | ConvertFrom-Json
Write-Host "Vector[1] (Bow Length Ratio): $($vector[1])" -ForegroundColor $(if ($vector[1] -gt 0) { "Green" } else { "Red" })
Write-Host "Vector[2] (Stern Length Ratio): $($vector[2])" -ForegroundColor $(if ($vector[2] -gt 0) { "Green" } else { "Red" })
Write-Host "Vector[20] (Service Speed): $($vector[20])" -ForegroundColor Yellow
Write-Host "Vector[31] (bit_BB): $($vector[31])" -ForegroundColor $(if ($vector[31] -gt 0) { "Green" } else { "Yellow" })

if ($vector[1] -gt 0 -and $vector[2] -gt 0) {
    Write-Host "`n✅ SUCCESS: Taxonomy defaults are being applied!" -ForegroundColor Green
}
else {
    Write-Host "`n❌ FAILED: Taxonomy defaults are NOT being applied" -ForegroundColor Red
}
