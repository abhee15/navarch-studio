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
        // Get vessel data
        var vessel = await _context.Vessels
            .Include(v => v.Stations)
            .Include(v => v.Waterlines)
            .Include(v => v.Offsets)
            .FirstOrDefaultAsync(v => v.Id == vesselId, cancellationToken);

        if (vessel == null)
        {
            throw new ArgumentException($"Vessel with ID {vesselId} not found");
        }

        // Get loadcase if specified
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

        // Get geometry
        var stations = vessel.Stations.OrderBy(s => s.StationIndex).ToList();
        var waterlines = vessel.Waterlines.OrderBy(w => w.WaterlineIndex).ToList();
        var offsets = vessel.Offsets.ToList();

        if (stations.Count == 0 || waterlines.Count == 0 || offsets.Count == 0)
        {
            throw new InvalidOperationException("Vessel geometry is incomplete");
        }

        // Filter waterlines up to specified draft
        var activeWaterlines = waterlines.Where(w => w.Z <= draft).ToList();
        if (activeWaterlines.Count < 2)
        {
            throw new ArgumentException($"Draft {draft} is below minimum waterline {waterlines[0].Z}");
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
        var waterplaneHalfBreadths = new List<decimal>();
        foreach (var station in stations)
        {
            var offset = offsets.FirstOrDefault(o =>
                o.StationIndex == station.StationIndex &&
                o.WaterlineIndex == activeWaterlines[^1].WaterlineIndex);
            waterplaneHalfBreadths.Add(offset?.HalfBreadthY ?? 0m);
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

        _logger.LogInformation(
            "Computed hydrostatics for vessel {VesselId} at draft {Draft}m: Vol={Volume}m³, Disp={Displacement}kg, KB={KB}m, LCB={LCB}m",
            vesselId, draft, volume, displacement, kb, lcb);

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
        var results = new List<HydroResultDto>();

        foreach (var draft in drafts)
        {
            var result = await ComputeAtDraftAsync(vesselId, loadcaseId, draft, cancellationToken);
            results.Add(result);
        }

        _logger.LogInformation(
            "Computed hydrostatic table for vessel {VesselId}: {Count} drafts",
            vesselId, results.Count);

        return results;
    }
}

