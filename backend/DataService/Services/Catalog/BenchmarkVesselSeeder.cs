using System.Text.Json;
using DataService.Data;
using DataService.Services.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Models;

namespace DataService.Services.Catalog;

/// <summary>
/// Service for seeding benchmark vessels with geometry from CSV files
///
/// UNITS: All vessel dimensions stored in SI base units (meters)
/// - Lpp, Beam, Draft: meters (m)
/// - Geometry (stations, waterlines, offsets): meters (m)
///
/// Unit conversion is handled at API boundaries via UnitConversionService
/// </summary>
public class BenchmarkVesselSeeder
{
    private readonly DataDbContext _context;
    private readonly BenchmarkGeometryImporter _geometryImporter;
    private readonly ILogger<BenchmarkVesselSeeder> _logger;

    public BenchmarkVesselSeeder(
        DataDbContext context,
        BenchmarkGeometryImporter geometryImporter,
        ILogger<BenchmarkVesselSeeder> logger)
    {
        _context = context;
        _geometryImporter = geometryImporter;
        _logger = logger;
    }

    /// <summary>
    /// Seeds benchmark vessels from CSV geometry files
    /// </summary>
    public async Task SeedBenchmarkVesselsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "Seeds",
                "benchmark-hulls");

            if (!Directory.Exists(baseDirectory))
            {
                _logger.LogWarning("Benchmark hulls directory not found at {Path}, skipping benchmark vessel seeding", baseDirectory);
                return;
            }

            // Define benchmark hull configurations
            var benchmarkConfigs = new[]
            {
                new BenchmarkConfig
                {
                    HullName = "Wigley",
                    CsvFileName = "wigley_offsets.csv",
                    VesselId = new Guid("00000000-0000-0000-0000-000000000002"),
                    Name = "Wigley Benchmark Hull",
                    Description = "Wigley parabolic hull form for benchmark testing. Analytical test hull with closed-form equation: y = (B/2) × (1 - z²) × (1 - x²)",
                    Lpp = 100m,
                    Beam = 10m,
                    Draft = 6.25m,
                    Cb = 0.444m,
                    HullFamily = "Parabolic",
                    CanonicalRef = "Wigley, W.G.S. (1942), Trans. RINA"
                },
                new BenchmarkConfig
                {
                    HullName = "Series60_like",
                    CsvFileName = "series60_like_offsets.csv",
                    VesselId = new Guid("00000000-0000-0000-0000-000000000003"),
                    Name = "Series 60-like Benchmark Hull",
                    Description = "Systematic series hull based on Series 60 parent form. Standard hull for resistance and propulsion studies.",
                    Lpp = 120m,
                    Beam = 16.25m,
                    Draft = 6.22m,
                    Cb = 0.70m,
                    HullFamily = "Series60",
                    CanonicalRef = "Todd, F.H. (1963), Series 60 hull forms"
                },
                new BenchmarkConfig
                {
                    HullName = "Prismatic_NPC",
                    CsvFileName = "prismatic_npc_offsets.csv",
                    VesselId = new Guid("00000000-0000-0000-0000-000000000004"),
                    Name = "Prismatic NPC Benchmark Hull",
                    Description = "Non-prismatic hull form for testing and validation purposes.",
                    Lpp = 100m,
                    Beam = 14m,
                    Draft = 5m,
                    Cb = 0.65m,
                    HullFamily = "Prismatic",
                    CanonicalRef = "Internal test form"
                }
            };

            foreach (var config in benchmarkConfigs)
            {
                await SeedBenchmarkVesselAsync(config, baseDirectory, cancellationToken);
            }

            _logger.LogInformation("Completed benchmark vessel seeding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding benchmark vessels");
            throw;
        }
    }

    private async Task SeedBenchmarkVesselAsync(
        BenchmarkConfig config,
        string baseDirectory,
        CancellationToken cancellationToken)
    {
        // Check if vessel already exists
        var existingVessel = await _context.Vessels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == config.VesselId, cancellationToken);

        if (existingVessel != null)
        {
            if (existingVessel.DeletedAt != null)
            {
                // Restore if soft-deleted
                existingVessel.DeletedAt = null;
                existingVessel.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Restored existing benchmark vessel {VesselId} ({Name})",
                    config.VesselId, config.Name);
            }
            else
            {
                _logger.LogInformation("Benchmark vessel {VesselId} ({Name}) already exists, skipping",
                    config.VesselId, config.Name);
            }
            return;
        }

        var csvPath = Path.Combine(baseDirectory, config.CsvFileName);
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("CSV file not found for {HullName} at {Path}, skipping",
                config.HullName, csvPath);
            return;
        }

        _logger.LogInformation("Seeding benchmark vessel {VesselId} ({Name}) from {FileName}...",
            config.VesselId, config.Name, config.CsvFileName);

        try
        {
            // Parse geometry from CSV
            using var csvStream = File.OpenRead(csvPath);
            var geometry = await _geometryImporter.ParseFromCsvAsync(
                csvStream,
                length: config.Lpp,
                beam: config.Beam,
                draft: config.Draft,
                hullName: config.HullName,
                cancellationToken);

            // Verify hull name matches
            if (geometry.HullName != config.HullName && geometry.HullName != "Wigley" && config.HullName == "Wigley")
            {
                // Wigley CSV might just say "Wigley" without "_like" suffix
                _logger.LogDebug("Hull name in CSV ({CsvName}) differs from expected ({Expected}), continuing",
                    geometry.HullName, config.HullName);
            }

            // Create vessel
            var vessel = new Vessel
            {
                Id = config.VesselId,
                UserId = TemplateVessels.SystemUserId,
                Name = config.Name,
                Description = config.Description,
                Lpp = config.Lpp,
                Beam = config.Beam,
                DesignDraft = config.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vessels.Add(vessel);

            // Create stations
            var stationIdMap = new Dictionary<int, Guid>(); // station_id -> Station.Id
            foreach (var stationRecord in geometry.Stations.OrderBy(s => s.Index))
            {
                var station = new Station
                {
                    Id = Guid.NewGuid(),
                    VesselId = vessel.Id,
                    StationIndex = stationRecord.Index,
                    X = stationRecord.X
                };
                _context.Stations.Add(station);
                stationIdMap[stationRecord.StationId] = station.Id;
            }

            // Create waterlines
            var waterlineIdMap = new Dictionary<int, Guid>(); // waterline_id -> Waterline.Id
            foreach (var waterlineRecord in geometry.Waterlines.OrderBy(w => w.Index))
            {
                var waterline = new Waterline
                {
                    Id = Guid.NewGuid(),
                    VesselId = vessel.Id,
                    WaterlineIndex = waterlineRecord.Index,
                    Z = waterlineRecord.Z
                };
                _context.Waterlines.Add(waterline);
                waterlineIdMap[waterlineRecord.WaterlineId] = waterline.Id;
            }

            // Create offsets
            // Need to map from CSV station_id/waterline_id to StationIndex/WaterlineIndex
            var stationIndexMap = geometry.Stations.ToDictionary(s => s.StationId, s => s.Index);
            var waterlineIndexMap = geometry.Waterlines.ToDictionary(w => w.WaterlineId, w => w.Index);

            foreach (var offsetRecord in geometry.Offsets)
            {
                if (!stationIndexMap.TryGetValue(offsetRecord.StationId, out var stationIndex))
                {
                    _logger.LogWarning("Offset references unknown station_id {StationId} for vessel {VesselId}",
                        offsetRecord.StationId, vessel.Id);
                    continue;
                }

                if (!waterlineIndexMap.TryGetValue(offsetRecord.WaterlineId, out var waterlineIndex))
                {
                    _logger.LogWarning("Offset references unknown waterline_id {WaterlineId} for vessel {VesselId}",
                        offsetRecord.WaterlineId, vessel.Id);
                    continue;
                }

                var offset = new Offset
                {
                    Id = Guid.NewGuid(),
                    VesselId = vessel.Id,
                    StationIndex = stationIndex,
                    WaterlineIndex = waterlineIndex,
                    HalfBreadthY = offsetRecord.HalfBreadthY
                };
                _context.Offsets.Add(offset);
            }

            // Create vessel metadata
            var metadata = new VesselMetadata
            {
                VesselId = vessel.Id,
                VesselType = "Benchmark",
                Size = "Standard",
                BlockCoefficient = config.Cb,
                HullFamily = config.HullFamily,
                CreatedAt = DateTime.UtcNow
            };
            _context.VesselMetadata.Add(metadata);

            // Create default loadcase
            var loadcase = new Loadcase
            {
                Id = Guid.NewGuid(),
                VesselId = vessel.Id,
                Name = "Design Condition",
                Rho = 1025m, // Saltwater density
                KG = config.Draft * 0.5m, // KG at 50% of draft
                Notes = $"Default load condition for {config.Name}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Loadcases.Add(loadcase);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully seeded benchmark vessel {VesselId} '{Name}' with {Stations} stations, {Waterlines} waterlines, {Offsets} offsets",
                vessel.Id, vessel.Name, geometry.Stations.Count, geometry.Waterlines.Count, geometry.Offsets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed benchmark vessel {VesselId} ({Name})",
                config.VesselId, config.Name);
            throw;
        }
    }

    private class BenchmarkConfig
    {
        public required string HullName { get; set; }
        public required string CsvFileName { get; set; }
        public required Guid VesselId { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required decimal Lpp { get; set; }
        public required decimal Beam { get; set; }
        public required decimal Draft { get; set; }
        public required decimal Cb { get; set; }
        public required string HullFamily { get; set; }
        public required string CanonicalRef { get; set; }
    }
}
