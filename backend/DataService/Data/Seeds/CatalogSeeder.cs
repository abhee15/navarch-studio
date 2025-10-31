using System.Text.Json;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.TestData;

namespace DataService.Data.Seeds;

/// <summary>
/// Seeds the catalog with reference data: hull geometries, water properties, and propeller series
/// </summary>
public class CatalogSeeder
{
    private readonly DataDbContext _context;
    private readonly ILogger<CatalogSeeder> _logger;

    public CatalogSeeder(DataDbContext context, ILogger<CatalogSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed all catalog data
    /// </summary>
    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting catalog seed...");

        await SeedWaterPropertiesAsync(cancellationToken);
        await SeedTemplateHullsAsync(cancellationToken);
        await SeedBenchmarkParticularsAsync(cancellationToken);

        _logger.LogInformation("Catalog seed complete.");
    }

    /// <summary>
    /// Seed ITTC water property anchor points (0, 15, 30 deg C for Fresh and Sea water)
    /// </summary>
    private async Task SeedWaterPropertiesAsync(CancellationToken cancellationToken)
    {
        if (await _context.CatalogWaterProperties.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Water properties already seeded, skipping");
            return;
        }

        var waterProps = new[]
        {
            // Freshwater (SA=0, p=0.101325 MPa)
            new CatalogWaterProperty
            {
                Medium = "Fresh",
                Temperature_C = 0,
                Salinity_PSU = 0,
                Density_kgm3 = 999.8425m,
                KinematicViscosity_m2s = 0.000001792m,
                SourceRef = "ITTC 7.5-02-01-03 Table 1"
            },
            new CatalogWaterProperty
            {
                Medium = "Fresh",
                Temperature_C = 15,
                Salinity_PSU = 0,
                Density_kgm3 = 999.1026m,
                KinematicViscosity_m2s = 0.000001139m,
                SourceRef = "ITTC 7.5-02-01-03 Table 1"
            },
            new CatalogWaterProperty
            {
                Medium = "Fresh",
                Temperature_C = 30,
                Salinity_PSU = 0,
                Density_kgm3 = 995.6502m,
                KinematicViscosity_m2s = 0.000000801m,
                SourceRef = "ITTC 7.5-02-01-03 Table 1"
            },

            // Seawater (SSW, SA≈35 g/kg, p=0.101325 MPa)
            new CatalogWaterProperty
            {
                Medium = "Sea",
                Temperature_C = 0,
                Salinity_PSU = 35,
                Density_kgm3 = 1028.106m,
                KinematicViscosity_m2s = 0.000001829m,
                SourceRef = "ITTC 7.5-02-01-03 Table 2"
            },
            new CatalogWaterProperty
            {
                Medium = "Sea",
                Temperature_C = 15,
                Salinity_PSU = 35,
                Density_kgm3 = 1025.970m,
                KinematicViscosity_m2s = 0.000001160m,
                SourceRef = "ITTC 7.5-02-01-03 Table 2"
            },
            new CatalogWaterProperty
            {
                Medium = "Sea",
                Temperature_C = 30,
                Salinity_PSU = 35,
                Density_kgm3 = 1021.830m,
                KinematicViscosity_m2s = 0.000000783m,
                SourceRef = "ITTC 7.5-02-01-03 Table 2"
            }
        };

        _context.CatalogWaterProperties.AddRange(waterProps);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} water property anchor points", waterProps.Length);
    }

    /// <summary>
    /// Seed template hulls with complete geometry (Wigley, Series60, Prismatic)
    /// Note: Actual geometry will be loaded from CSV files in a future update
    /// </summary>
    private async Task SeedTemplateHullsAsync(CancellationToken cancellationToken)
    {
        if (await _context.BenchmarkCases.AnyAsync(b => b.HullType == "Template", cancellationToken))
        {
            _logger.LogInformation("Template hulls already seeded, skipping");
            return;
        }

        // For now, create catalog entries without geometry (geometry_missing=false but StationsJson etc. not populated)
        // TODO: Load actual geometry from CSV files in Phase 3.1
        var templates = new[]
        {
            new BenchmarkCase
            {
                Slug = "wigley-hull",
                Title = "Wigley Hull",
                Description = "Analytic test hull with closed-form equation: y = (B/2) × (1 - z²) × (1 - x²)",
                HullType = "Template",
                GeometryMissing = true, // Will be set to false once geometry is loaded
                CanonicalRefs = "Wigley, W.G.S. (1942), Trans. RINA",
                CreatedAt = DateTime.UtcNow
            },
            new BenchmarkCase
            {
                Slug = "series60-like",
                Title = "Series 60-like Hull",
                Description = "Systematic series hull based on series 60 parent form",
                HullType = "Template",
                GeometryMissing = true,
                CanonicalRefs = "Todd, F.H. (1963), Series 60 hull forms",
                CreatedAt = DateTime.UtcNow
            },
            new BenchmarkCase
            {
                Slug = "prismatic-npc",
                Title = "Prismatic NPC Hull",
                Description = "Non-prismatic hull form for testing",
                HullType = "Template",
                GeometryMissing = true,
                CanonicalRefs = "Internal test form",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.BenchmarkCases.AddRange(templates);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} template hulls", templates.Length);

        // Add geometry to Wigley hull
        await SeedWigleyGeometryAsync(cancellationToken);
    }

    /// <summary>
    /// Seed Wigley hull geometry using analytical formula
    /// </summary>
    private async Task SeedWigleyGeometryAsync(CancellationToken cancellationToken)
    {
        var wigley = await _context.BenchmarkCases
            .Include(c => c.Geometries)
            .FirstOrDefaultAsync(c => c.Slug == "wigley-hull", cancellationToken);

        if (wigley == null)
        {
            _logger.LogWarning("Wigley hull not found, skipping geometry seed");
            return;
        }

        // Check if geometry already exists
        if (wigley.Geometries.Any())
        {
            _logger.LogInformation("Wigley geometry already seeded, skipping");
            return;
        }

        // Generate Wigley hull with standard dimensions: L=100m, B=10m, T=6.25m
        var (stations, waterlines, offsets) = HullTestData.GenerateWigleyHull(
            length: 100m,
            beam: 10m,
            designDraft: 6.25m,
            numStations: 21,
            numWaterlines: 13);

        // Serialize to JSON
        var stationsJson = JsonSerializer.Serialize(stations.Select(s => new { s.Index, s.X }));
        var waterlinesJson = JsonSerializer.Serialize(waterlines.Select(w => new { w.Index, w.Z }));
        var offsetsJson = JsonSerializer.Serialize(offsets.Select(o => new { o.StationIndex, o.WaterlineIndex, o.HalfBreadthY }));

        var geometry = new BenchmarkGeometry
        {
            Id = Guid.NewGuid(),
            CaseId = wigley.Id,
            Type = "analytic",
            SourceUrl = "Analytical formula",
            StationsJson = stationsJson,
            WaterlinesJson = waterlinesJson,
            OffsetsJson = offsetsJson,
            CreatedAt = DateTime.UtcNow
        };

        _context.BenchmarkGeometries.Add(geometry);

        // Mark as complete
        wigley.GeometryMissing = false;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded Wigley hull geometry: {Stations} stations, {Waterlines} waterlines, {Offsets} offsets",
            stations.Count, waterlines.Count, offsets.Count);
    }

    /// <summary>
    /// Seed benchmark hull particulars only (KCS, KVLCC2, DTMB 5415)
    /// Geometry will be added later via IGES/offsets import
    /// </summary>
    private async Task SeedBenchmarkParticularsAsync(CancellationToken cancellationToken)
    {
        if (await _context.BenchmarkCases.AnyAsync(b => b.HullType != null && b.HullType != "Template", cancellationToken))
        {
            _logger.LogInformation("Benchmark particulars already seeded, skipping");
            return;
        }

        var benchmarks = new[]
        {
            new BenchmarkCase
            {
                Slug = "kcs",
                Title = "KRISO Container Ship (KCS)",
                Description = "Standard container ship hull for resistance and validation studies",
                HullType = "Container",
                Lpp_m = 230.0m,
                B_m = 32.2m,
                T_m = 10.8m,
                Cb = 0.651m,
                Cp = 0.664m,
                LCB_pctLpp = -1.5m,
                LCF_pctLpp = 0.0m,
                GeometryMissing = true,
                CanonicalRefs = "SIMMAN 2008, Workshop on Verification and Validation of Ship CFD (2008)",
                CreatedAt = DateTime.UtcNow
            },
            new BenchmarkCase
            {
                Slug = "kvlcc2",
                Title = "KVLCC2 Very Large Crude Carrier",
                Description = "Tanker hull form for resistance and viscous flow validation",
                HullType = "Tanker",
                Lpp_m = 320.0m,
                B_m = 58.0m,
                T_m = 20.8m,
                Cb = 0.8098m,
                Cp = 0.7845m,
                LCB_pctLpp = -2.0m,
                LCF_pctLpp = 0.0m,
                GeometryMissing = true,
                CanonicalRefs = "SIMMAN 2008 Workshop, KRISO",
                CreatedAt = DateTime.UtcNow
            },
            new BenchmarkCase
            {
                Slug = "dtmb-5415",
                Title = "DTMB 5415 Naval Combatant",
                Description = "Destroyer-class hull for hydrodynamic and CFD validation",
                HullType = "Naval",
                Lpp_m = 141.8m,  // Actual full-scale length
                B_m = 19.06m,
                T_m = 7.18m,
                Cb = 0.507m,
                Cp = 0.603m,
                LCB_pctLpp = -0.7m,
                LCF_pctLpp = 0.0m,
                GeometryMissing = true,
                CanonicalRefs = "SIMMAN 2008, DTMB/NSWC",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.BenchmarkCases.AddRange(benchmarks);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} benchmark hull particulars", benchmarks.Length);
    }
}
