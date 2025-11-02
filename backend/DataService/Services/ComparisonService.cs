using System.Text.Json;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;
using Shared.Services;

namespace DataService.Services;

/// <summary>
/// Service for managing comparison snapshots and computing deltas between runs
/// </summary>
public class ComparisonService
{
    private readonly DataDbContext _context;
    private readonly IUnitConversionService _unitConversionService;

    public ComparisonService(DataDbContext context, IUnitConversionService unitConversionService)
    {
        _context = context;
        _unitConversionService = unitConversionService;
    }

    /// <summary>
    /// Create a new comparison snapshot
    /// </summary>
    public async Task<ComparisonSnapshot> CreateSnapshotAsync(
        Guid vesselId,
        Guid userId,
        CreateComparisonSnapshotDto dto,
        string displayUnits,
        CancellationToken cancellationToken = default)
    {
        // Get vessel and loadcase for snapshotting
        var vessel = await _context.Vessels
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken)
            ?? throw new InvalidOperationException($"Vessel {vesselId} not found");

        Loadcase? loadcase = null;
        if (dto.LoadcaseId.HasValue)
        {
            loadcase = await _context.Loadcases
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == dto.LoadcaseId.Value, cancellationToken);
        }

        // Convert inputs from display units to SI for storage
        var minDraftSI = ConvertToSI(dto.MinDraft, displayUnits, "Length");
        var maxDraftSI = ConvertToSI(dto.MaxDraft, displayUnits, "Length");
        var draftStepSI = ConvertToSI(dto.DraftStep, displayUnits, "Length");

        // Convert results to SI for storage
        var resultsInSI = dto.Results.Select(r => ConvertResultToSI(r, displayUnits)).ToList();

        // Serialize results to JSON
        var resultsJson = JsonSerializer.Serialize(resultsInSI);

        var snapshot = new ComparisonSnapshot
        {
            UserId = userId,
            VesselId = vesselId,
            LoadcaseId = dto.LoadcaseId,
            RunName = dto.RunName,
            Description = dto.Description,
            IsBaseline = dto.IsBaseline,
            Tags = dto.Tags,
            VesselLpp = vessel.Lpp,
            VesselBeam = vessel.Beam,
            VesselDesignDraft = vessel.DesignDraft,
            LoadcaseRho = loadcase?.Rho,
            LoadcaseKG = loadcase?.KG,
            MinDraft = minDraftSI,
            MaxDraft = maxDraftSI,
            DraftStep = draftStepSI,
            ResultsJson = resultsJson,
            ComputationTimeMs = dto.ComputationTimeMs
        };

        _context.ComparisonSnapshots.Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    /// <summary>
    /// Get all snapshots for a vessel
    /// </summary>
    public async Task<List<ComparisonSnapshotDto>> GetSnapshotsAsync(
        Guid vesselId,
        string displayUnits,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await _context.ComparisonSnapshots
            .Include(s => s.Vessel)
            .Include(s => s.Loadcase)
            .Where(s => s.VesselId == vesselId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = new List<ComparisonSnapshotDto>();

        foreach (var snapshot in snapshots)
        {
            var dto = await ConvertSnapshotToDtoAsync(snapshot, displayUnits);
            dtos.Add(dto);
        }

        return dtos;
    }

    /// <summary>
    /// Get a single snapshot by ID
    /// </summary>
    public async Task<ComparisonSnapshotDto?> GetSnapshotByIdAsync(
        Guid snapshotId,
        string displayUnits,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.ComparisonSnapshots
            .Include(s => s.Vessel)
            .Include(s => s.Loadcase)
            .FirstOrDefaultAsync(s => s.Id == snapshotId, cancellationToken);

        if (snapshot == null) return null;

        return await ConvertSnapshotToDtoAsync(snapshot, displayUnits);
    }

    /// <summary>
    /// Update a snapshot (e.g., mark as baseline)
    /// </summary>
    public async Task<ComparisonSnapshot?> UpdateSnapshotAsync(
        Guid snapshotId,
        UpdateComparisonSnapshotDto dto,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.ComparisonSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshotId, cancellationToken);

        if (snapshot == null) return null;

        if (dto.RunName != null) snapshot.RunName = dto.RunName;
        if (dto.Description != null) snapshot.Description = dto.Description;
        if (dto.Tags != null) snapshot.Tags = dto.Tags;

        // If setting as baseline, unmark other baselines for this vessel
        if (dto.IsBaseline == true)
        {
            var otherBaselines = await _context.ComparisonSnapshots
                .Where(s => s.VesselId == snapshot.VesselId && s.Id != snapshotId && s.IsBaseline)
                .ToListAsync(cancellationToken);

            foreach (var other in otherBaselines)
            {
                other.IsBaseline = false;
            }

            snapshot.IsBaseline = true;
        }
        else if (dto.IsBaseline == false)
        {
            snapshot.IsBaseline = false;
        }

        snapshot.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return snapshot;
    }

    /// <summary>
    /// Delete a snapshot
    /// </summary>
    public async Task<bool> DeleteSnapshotAsync(Guid snapshotId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.ComparisonSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshotId, cancellationToken);

        if (snapshot == null) return false;

        snapshot.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Compare two snapshots and compute deltas
    /// </summary>
    public async Task<ComparisonReportDto> CompareSnapshotsAsync(
        Guid baselineId,
        Guid candidateId,
        string displayUnits,
        CancellationToken cancellationToken = default)
    {
        var baseline = await GetSnapshotByIdAsync(baselineId, displayUnits, cancellationToken)
            ?? throw new InvalidOperationException($"Baseline snapshot {baselineId} not found");

        var candidate = await GetSnapshotByIdAsync(candidateId, displayUnits, cancellationToken)
            ?? throw new InvalidOperationException($"Candidate snapshot {candidateId} not found");

        // Build draft-by-draft comparisons
        var draftComparisons = new List<DraftComparisonDto>();

        // Match drafts between baseline and candidate (use baseline drafts as reference)
        foreach (var baselineResult in baseline.Results)
        {
            // Find closest matching draft in candidate (within tolerance)
            var candidateResult = candidate.Results
                .OrderBy(r => Math.Abs(r.Draft - baselineResult.Draft))
                .FirstOrDefault(r => Math.Abs(r.Draft - baselineResult.Draft) < 0.1m);

            if (candidateResult == null) continue;

            var kpiComparisons = new List<KpiComparisonDto>
            {
                CompareKpi("Displacement", baselineResult.DispWeight, candidateResult.DispWeight, "Higher"),
                CompareKpi("KB", baselineResult.KBz, candidateResult.KBz, "Higher"),
                CompareKpi("LCB", baselineResult.LCBx, candidateResult.LCBx, "Neutral"),
                CompareKpi("BMt", baselineResult.BMt, candidateResult.BMt, "Higher"),
                CompareKpi("GMt", baselineResult.GMt, candidateResult.GMt, "Higher"),
                CompareKpi("WPA", baselineResult.Awp, candidateResult.Awp, "Higher"),
                CompareKpi("Cb", baselineResult.Cb, candidateResult.Cb, "Neutral"),
                CompareKpi("Cp", baselineResult.Cp, candidateResult.Cp, "Neutral"),
                CompareKpi("Cwp", baselineResult.Cwp, candidateResult.Cwp, "Neutral"),
            };

            draftComparisons.Add(new DraftComparisonDto
            {
                Draft = baselineResult.Draft,
                KpiComparisons = kpiComparisons
            });
        }

        // Summary comparisons (at design draft or mid-range)
        var summaryComparisons = draftComparisons.FirstOrDefault()?.KpiComparisons ?? new List<KpiComparisonDto>();

        return new ComparisonReportDto
        {
            Baseline = baseline,
            Candidate = candidate,
            SummaryComparisons = summaryComparisons,
            DraftComparisons = draftComparisons
        };
    }

    // Helper: Convert snapshot entity to DTO
    private async Task<ComparisonSnapshotDto> ConvertSnapshotToDtoAsync(ComparisonSnapshot snapshot, string displayUnits)
    {
        // Deserialize results from JSON (stored in SI)
        var resultsInSI = JsonSerializer.Deserialize<List<HydroResultDto>>(snapshot.ResultsJson) ?? new List<HydroResultDto>();

        // Convert results to display units using unit conversion service
        var resultsInDisplay = new List<HydroResultDto>();
        foreach (var resultSI in resultsInSI)
        {
            var resultDisplay = _unitConversionService.ConvertDto(resultSI, "SI", displayUnits);
            resultsInDisplay.Add(resultDisplay);
        }

        // Create DTO and convert unit-aware fields
        var dto = new ComparisonSnapshotDto
        {
            Id = snapshot.Id,
            VesselId = snapshot.VesselId,
            VesselName = snapshot.Vessel.Name,
            LoadcaseId = snapshot.LoadcaseId,
            LoadcaseName = snapshot.Loadcase?.Name,
            RunName = snapshot.RunName,
            Description = snapshot.Description,
            IsBaseline = snapshot.IsBaseline,
            Tags = snapshot.Tags,
            VesselLpp = snapshot.VesselLpp,
            VesselBeam = snapshot.VesselBeam,
            VesselDesignDraft = snapshot.VesselDesignDraft,
            LoadcaseRho = snapshot.LoadcaseRho,
            LoadcaseKG = snapshot.LoadcaseKG,
            MinDraft = snapshot.MinDraft,
            MaxDraft = snapshot.MaxDraft,
            DraftStep = snapshot.DraftStep,
            Results = resultsInDisplay,
            ComputationTimeMs = snapshot.ComputationTimeMs,
            CreatedAt = snapshot.CreatedAt
        };

        // Convert the DTO itself from SI to display units
        dto = _unitConversionService.ConvertDto(dto, "SI", displayUnits);

        return dto;
    }

    // Helper: Compare a single KPI
    private KpiComparisonDto CompareKpi(
        string kpiName,
        decimal? baselineValue,
        decimal? candidateValue,
        string betterDirection)
    {
        decimal? absDelta = null;
        decimal? pctDelta = null;
        string? interpretation = null;

        if (baselineValue.HasValue && candidateValue.HasValue)
        {
            absDelta = candidateValue.Value - baselineValue.Value;
            if (baselineValue.Value != 0)
            {
                pctDelta = (absDelta.Value / baselineValue.Value) * 100m;
            }

            // Determine interpretation
            if (absDelta.Value > 0)
            {
                interpretation = betterDirection == "Higher" ? "Better" : (betterDirection == "Lower" ? "Worse" : "Neutral");
            }
            else if (absDelta.Value < 0)
            {
                interpretation = betterDirection == "Higher" ? "Worse" : (betterDirection == "Lower" ? "Better" : "Neutral");
            }
            else
            {
                interpretation = "Unchanged";
            }
        }

        return new KpiComparisonDto
        {
            KpiName = kpiName,
            Unit = "", // Unit will be determined by frontend based on display units
            BaselineValue = baselineValue,
            CandidateValue = candidateValue,
            AbsoluteDelta = absDelta,
            PercentDelta = pctDelta,
            Interpretation = interpretation
        };
    }

    // Helper: Simple unit conversion (for non-DTO values)
    private decimal ConvertToSI(decimal value, string fromUnits, string dimension)
    {
        // Create a simple DTO to leverage unit conversion service
        if (fromUnits == "SI") return value;

        // For simplicity, use conversion factors
        // This is a simplified approach - in production, use full unit converter
        if (dimension == "Length")
        {
            if (fromUnits == "Imperial") return value * 0.3048m; // feet to meters
        }

        return value;
    }

    // Helper: Convert result from display units to SI
    private HydroResultDto ConvertResultToSI(HydroResultDto result, string fromUnits)
    {
        if (fromUnits == "SI") return result;

        // Use unit conversion service
        return _unitConversionService.ConvertDto(result, fromUnits, "SI");
    }
}
