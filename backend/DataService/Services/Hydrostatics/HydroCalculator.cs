using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of hydrostatic calculator
/// </summary>
public class HydroCalculator : IHydroCalculator
{
    private readonly DataDbContext _context;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly ILogger<HydroCalculator> _logger;

    public HydroCalculator(
        DataDbContext context,
        IIntegrationEngine integrationEngine,
        ILogger<HydroCalculator> logger)
    {
        _context = context;
        _integrationEngine = integrationEngine;
        _logger = logger;
    }

    public async Task<HydroResultDto> ComputeAtDraftAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal draft,
        CancellationToken cancellationToken = default)
    {
        // Load vessel geometry and compute
        var vessel = await GetVesselWithGeometryAsync(vesselId, cancellationToken);
        var (rho, kg) = await GetLoadcaseParametersAsync(loadcaseId, cancellationToken);

        return ComputeAtDraftCore(vessel, rho, kg, draft);
    }

    /// <summary>
    /// Core computation method that works with pre-loaded data
    /// </summary>
    private HydroResultDto ComputeAtDraftCore(
        VesselWithGeometry vessel,
        decimal rho,
        decimal? kg,
        decimal draft)
    {
        // Get geometry
        var stations = vessel.Stations;
        var waterlines = vessel.Waterlines;
        var offsets = vessel.Offsets;

        if (stations.Count == 0 || waterlines.Count == 0 || offsets.Count == 0)
        {
            throw new InvalidOperationException("Vessel geometry is incomplete");
        }

        // Filter waterlines up to specified draft
        var activeWaterlines = waterlines.Where(w => w.Z <= draft).ToList();
        if (activeWaterlines.Count < 2)
        {
            throw new ArgumentException($"Draft {draft} is below minimum waterline {waterlines.FirstOrDefault()?.Z ?? 0}");
        }

        // 1. Compute sectional areas at each station
        var sectionAreas = new List<decimal>();
        var sectionCentroids = new List<decimal>(); // Z-centroids of each section
        var stationXPositions = new List<decimal>();

        foreach (var station in stations)
        {
            stationXPositions.Add(station.X);

            // Get half-breadths for this station at each active waterline
            var halfBreadths = new List<decimal>();
            var waterlineZs = new List<decimal>();

            foreach (var wl in activeWaterlines)
            {
                var offset = offsets.FirstOrDefault(o =>
                    o.StationIndex == station.StationIndex &&
                    o.WaterlineIndex == wl.WaterlineIndex);

                waterlineZs.Add(wl.Z);
                halfBreadths.Add(offset?.HalfBreadthY ?? 0m);
            }

            // Integrate to get area of this section (half-section * 2)
            var halfSectionArea = _integrationEngine.Integrate(waterlineZs, halfBreadths);
            var fullSectionArea = 2 * halfSectionArea; // Mirror to port side
            sectionAreas.Add(fullSectionArea);

            // Compute Z-centroid of section: ∫ z * y dz / ∫ y dz
            if (halfSectionArea > 0)
            {
                var firstMoment = _integrationEngine.FirstMoment(waterlineZs, halfBreadths);
                var centroidZ = firstMoment / halfSectionArea;
                sectionCentroids.Add(centroidZ);
            }
            else
            {
                sectionCentroids.Add(0);
            }
        }

        // 2. Compute volume by integrating sectional areas along length
        var volume = _integrationEngine.Integrate(stationXPositions, sectionAreas);
        var displacement = volume * rho;

        // 3. Compute KB (vertical center of buoyancy)
        var kbMoment = 0m;
        for (int i = 0; i < sectionAreas.Count; i++)
        {
            kbMoment += sectionCentroids[i] * sectionAreas[i];
        }
        var kbVolume = _integrationEngine.Integrate(stationXPositions,
            sectionAreas.Select((a, i) => sectionCentroids[i] * a).ToList());
        var kb = volume > 0 ? kbVolume / volume : 0;

        // 4. Compute LCB (longitudinal center of buoyancy)
        var lcbMoment = _integrationEngine.FirstMoment(stationXPositions, sectionAreas);
        var lcb = volume > 0 ? lcbMoment / volume : 0;

        // 5. Compute TCB (transverse center of buoyancy) - should be 0 for symmetric hull
        var tcb = 0m; // Assuming port/starboard symmetry

        // 6. Compute waterplane area and second moments at current draft
        // Need to interpolate half-breadths at exact draft (not just use last active waterline)
        var waterplaneHalfBreadths = new List<decimal>();

        // Check if draft is very close to the last active waterline (within 0.1%)
        var lastActiveWL = activeWaterlines[^1];
        var useExactWaterline = Math.Abs(draft - lastActiveWL.Z) / draft < 0.001m;

        foreach (var station in stations)
        {
            decimal halfBreadthAtDraft;

            if (useExactWaterline)
            {
                // Draft is essentially at a waterline, use exact values
                var offset = offsets.FirstOrDefault(o =>
                    o.StationIndex == station.StationIndex &&
                    o.WaterlineIndex == lastActiveWL.WaterlineIndex);
                halfBreadthAtDraft = offset?.HalfBreadthY ?? 0m;
            }
            else
            {
                // Interpolate between waterlines
                halfBreadthAtDraft = InterpolateHalfBreadthAtDraft(
                    station.StationIndex, draft, waterlines, offsets);
            }

            waterplaneHalfBreadths.Add(halfBreadthAtDraft);
        }

        var awp = 2 * _integrationEngine.Integrate(stationXPositions, waterplaneHalfBreadths);

        // Transverse second moment: I_t = ∫ (2/3 * y³) dx along length
        // For each station, compute (2/3 * y³), then integrate along x
        var transverseInertiaPerStation = waterplaneHalfBreadths
            .Select(y => (2m / 3m) * y * y * y)
            .ToList();
        var transverseSecondMoment = _integrationEngine.Integrate(stationXPositions, transverseInertiaPerStation);

        // Longitudinal second moment: I_l = 2 * ∫ x² * y dx
        var longitudinalSecondMoment = 2 * _integrationEngine.SecondMoment(
            stationXPositions,
            waterplaneHalfBreadths);

        var iwp = transverseSecondMoment; // Store transverse moment as Iwp

        // 7. Compute metacentric radii
        var bmt = volume > 0 ? transverseSecondMoment / volume : 0;
        var bml = volume > 0 ? longitudinalSecondMoment / volume : 0;

        // 8. Compute metacentric heights (if KG is provided)
        decimal? gmt = null;
        decimal? gml = null;
        if (kg.HasValue)
        {
            var kmt = kb + bmt;
            var kml = kb + bml;
            gmt = kmt - kg.Value;
            gml = kml - kg.Value;
        }

        // 9. Compute form coefficients
        var midshipIndex = stations.Count / 2;
        var midshipArea = sectionAreas[midshipIndex];

        var cb = (vessel.Lpp * vessel.Beam * draft) > 0
            ? volume / (vessel.Lpp * vessel.Beam * draft)
            : 0;

        var cp = (midshipArea * vessel.Lpp) > 0
            ? volume / (midshipArea * vessel.Lpp)
            : 0;

        var cm = (vessel.Beam * draft) > 0
            ? midshipArea / (vessel.Beam * draft)
            : 0;

        var cwp = (vessel.Lpp * vessel.Beam) > 0
            ? awp / (vessel.Lpp * vessel.Beam)
            : 0;

        _logger.LogDebug(
            "Computed hydrostatics at draft {Draft}m: Vol={Volume}m³, Disp={Displacement}kg, KB={KB}m, LCB={LCB}m",
            draft, volume, displacement, kb, lcb);

        return new HydroResultDto
        {
            Draft = draft,
            DispVolume = volume,
            DispWeight = displacement,
            KBz = kb,
            LCBx = lcb,
            TCBy = tcb,
            BMt = bmt,
            BMl = bml,
            GMt = gmt,
            GMl = gml,
            Awp = awp,
            Iwp = iwp,
            Cb = cb,
            Cp = cp,
            Cm = cm,
            Cwp = cwp,
            TrimAngle = null // Not computed in this basic version
        };
    }

    public async Task<List<HydroResultDto>> ComputeTableAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<decimal> drafts,
        CancellationToken cancellationToken = default)
    {
        // Load vessel geometry and loadcase once for all drafts (optimization)
        var vessel = await GetVesselWithGeometryAsync(vesselId, cancellationToken);
        var (rho, kg) = await GetLoadcaseParametersAsync(loadcaseId, cancellationToken);

        var results = new List<HydroResultDto>();

        foreach (var draft in drafts)
        {
            var result = ComputeAtDraftCore(vessel, rho, kg, draft);
            results.Add(result);
        }

        _logger.LogInformation(
            "Computed hydrostatic table for vessel {VesselId}: {Count} drafts",
            vesselId, results.Count);

        return results;
    }

    /// <summary>
    /// Helper method to load vessel geometry once
    /// </summary>
    private async Task<VesselWithGeometry> GetVesselWithGeometryAsync(
        Guid vesselId,
        CancellationToken cancellationToken)
    {
        var vessel = await _context.Vessels
            .Include(v => v.Stations)
            .Include(v => v.Waterlines)
            .Include(v => v.Offsets)
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        return new VesselWithGeometry
        {
            Lpp = vessel.Lpp,
            Beam = vessel.Beam,
            Stations = vessel.Stations.OrderBy(s => s.StationIndex).ToList(),
            Waterlines = vessel.Waterlines.OrderBy(w => w.WaterlineIndex).ToList(),
            Offsets = vessel.Offsets.ToList()
        };
    }

    /// <summary>
    /// Helper method to get loadcase parameters
    /// </summary>
    private async Task<(decimal rho, decimal? kg)> GetLoadcaseParametersAsync(
        Guid? loadcaseId,
        CancellationToken cancellationToken)
    {
        decimal rho = 1025m; // Default saltwater density
        decimal? kg = null;

        if (loadcaseId.HasValue)
        {
            var loadcase = await _context.Loadcases.FindAsync(new object[] { loadcaseId.Value }, cancellationToken);
            if (loadcase != null)
            {
                rho = loadcase.Rho;
                kg = loadcase.KG;
            }
        }

        return (rho, kg);
    }

    /// <summary>
    /// Interpolates half-breadth at a specific draft
    /// </summary>
    private decimal InterpolateHalfBreadthAtDraft(
        int stationIndex,
        decimal draft,
        List<Shared.Models.Waterline> waterlines,
        List<Shared.Models.Offset> offsets)
    {
        const decimal tolerance = 0.0001m; // Tolerance for decimal comparison

        // Find waterlines immediately above and below the draft
        var wlBelow = waterlines
            .Where(w => w.Z <= draft + tolerance) // Add tolerance for floating point comparison
            .OrderByDescending(w => w.Z)
            .FirstOrDefault();

        var wlAbove = waterlines
            .Where(w => w.Z >= draft - tolerance) // Add tolerance
            .OrderBy(w => w.Z)
            .FirstOrDefault();

        // If draft matches a waterline exactly (within tolerance)
        if (wlBelow != null && Math.Abs(wlBelow.Z - draft) < tolerance)
        {
            var offset = offsets.FirstOrDefault(o =>
                o.StationIndex == stationIndex &&
                o.WaterlineIndex == wlBelow.WaterlineIndex);
            return offset?.HalfBreadthY ?? 0m;
        }

        if (wlAbove != null && Math.Abs(wlAbove.Z - draft) < tolerance)
        {
            var offset = offsets.FirstOrDefault(o =>
                o.StationIndex == stationIndex &&
                o.WaterlineIndex == wlAbove.WaterlineIndex);
            return offset?.HalfBreadthY ?? 0m;
        }

        // If only one waterline found, use it
        if (wlBelow == null && wlAbove != null)
        {
            var offset = offsets.FirstOrDefault(o =>
                o.StationIndex == stationIndex &&
                o.WaterlineIndex == wlAbove.WaterlineIndex);
            return offset?.HalfBreadthY ?? 0m;
        }

        if (wlAbove == null && wlBelow != null)
        {
            var offset = offsets.FirstOrDefault(o =>
                o.StationIndex == stationIndex &&
                o.WaterlineIndex == wlBelow.WaterlineIndex);
            return offset?.HalfBreadthY ?? 0m;
        }

        // Interpolate between two waterlines
        if (wlBelow != null && wlAbove != null && Math.Abs(wlBelow.Z - wlAbove.Z) > tolerance)
        {
            var offsetBelow = offsets.FirstOrDefault(o =>
                o.StationIndex == stationIndex &&
                o.WaterlineIndex == wlBelow.WaterlineIndex);

            var offsetAbove = offsets.FirstOrDefault(o =>
                o.StationIndex == stationIndex &&
                o.WaterlineIndex == wlAbove.WaterlineIndex);

            var yBelow = offsetBelow?.HalfBreadthY ?? 0m;
            var yAbove = offsetAbove?.HalfBreadthY ?? 0m;

            // Linear interpolation
            var fraction = (draft - wlBelow.Z) / (wlAbove.Z - wlBelow.Z);
            return yBelow + fraction * (yAbove - yBelow);
        }

        return 0m;
    }

    /// <summary>
    /// Internal data structure to hold vessel geometry
    /// </summary>
    private class VesselWithGeometry
    {
        public decimal Lpp { get; init; }
        public decimal Beam { get; init; }
        public List<Shared.Models.Station> Stations { get; init; } = new();
        public List<Shared.Models.Waterline> Waterlines { get; init; } = new();
        public List<Shared.Models.Offset> Offsets { get; init; } = new();
    }
}
