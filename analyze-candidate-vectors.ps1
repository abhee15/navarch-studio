# Analyze ShipD vectors for candidates to understand why #1/#2 look similar
$baseUrl = "http://localhost:5004/api/v1"
$runId = "0e00f2a1-8c28-4583-a289-02361446080c"

Write-Host "`n=== CANDIDATE SHIPD VECTOR ANALYSIS ===" -ForegroundColor Cyan
Write-Host "Run ID: $runId`n" -ForegroundColor Gray

try {
    $candidates = Invoke-RestMethod -Uri "$baseUrl/hull-sizing/runs/$runId/candidate-designs" -Method GET

    foreach ($c in $candidates | Sort-Object rank) {
        Write-Host "Candidate #$($c.rank):" -ForegroundColor Yellow
        Write-Host "  Lpp: $($c.lppM)m, Beam: $($c.beamM)m, Draft: $($c.draftM)m, CB: $($c.cb)" -ForegroundColor White

        if ($c.shipdParametersJson) {
            $vector = $c.shipdParametersJson | ConvertFrom-Json

            $bowRatio = $vector[1]
            $sternRatio = $vector[2]
            $bitBB = $vector[31]

            Write-Host "  ShipD Vector[1] (Bow Ratio): $bowRatio" -ForegroundColor $(if ($bowRatio -eq 0.30) { 'Red' } else { 'Green' })
            Write-Host "  ShipD Vector[2] (Stern Ratio): $sternRatio" -ForegroundColor $(if ($sternRatio -eq 0.30) { 'Red' } else { 'Green' })
            Write-Host "  ShipD Vector[31] (bit_BB): $bitBB" -ForegroundColor White

            # Calculate midship ratio
            $midshipRatio = 1.0 - $bowRatio - $sternRatio
            Write-Host "  Calculated Midship Ratio: $midshipRatio" -ForegroundColor Cyan

            # Count non-zero parameters
            $nonZeroCount = ($vector | Where-Object { $_ -ne 0 }).Count
            Write-Host "  Non-zero parameters: $nonZeroCount/45" -ForegroundColor Gray
        } else {
            Write-Host "  ShipD Vector: NULL" -ForegroundColor Red
        }
        Write-Host ""
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
