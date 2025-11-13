using System.Collections.Generic;
using System.Linq;
using Shared.DTOs.ShipD;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Implementation of ShipD hull geometry generation service.
/// Converts ShipD 45-parameter vectors into actual hull geometry.
/// </summary>
public class ShipDHullGeometryService : IShipDHullGeometryService
{
    private readonly ILogger<ShipDHullGeometryService> _logger;

    public ShipDHullGeometryService(ILogger<ShipDHullGeometryService> logger)
    {
        _logger = logger;
    }

    public Task<HullSectionsDto> GenerateSectionsAsync(
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        int stationCount = 20,
        CancellationToken cancellationToken = default)
    {
        if (shipdVector == null || shipdVector.Length != 45)
        {
            throw new ArgumentException("ShipD vector must contain exactly 45 parameters", nameof(shipdVector));
        }

        _logger.LogDebug(
            "[SHIPD_GEOMETRY] Generating {StationCount} sections for Lpp={Lpp}m, Beam={Beam}m, Draft={Draft}m",
            stationCount, lppM, beamM, draftM);

        // Denormalize key parameters
        var denormalized = DenormalizeParameters(shipdVector, metadata);

        // Extract longitudinal proportions
        var lb = denormalized[1]; // Bow length ratio
        var ls = denormalized[2]; // Stern length ratio
        var lm = 1.0m - lb - ls; // Mid-body length ratio

        // Calculate boundaries
        var bowStart = ls + lm; // Start of bow region (0 = aft)
        var midStart = ls; // Start of mid-body region

        var stations = new List<HullStationDto>();
        var stationPositions = new List<decimal>();

        // Generate stations
        for (int i = 0; i < stationCount; i++)
        {
            var stationPos = (decimal)i / (stationCount - 1); // 0 = aft, 1 = forward
            stationPositions.Add(stationPos);

            // Determine which region this station is in
            var region = stationPos < midStart
                ? "stern"
                : stationPos < bowStart
                    ? "midship"
                    : "bow";

            // Generate offsets for this station
            var offsets = GenerateStationOffsets(
                stationPos,
                region,
                denormalized,
                lppM,
                beamM,
                draftM,
                metadata);

            // Check for bulb (only in bow region)
            var hasBulb = region == "bow" && denormalized[31] > 0.5m; // bit_BB
            Dictionary<decimal, decimal>? bulbOffsets = null;

            if (hasBulb)
            {
                bulbOffsets = GenerateBulbOffsets(
                    stationPos,
                    denormalized,
                    lppM,
                    beamM,
                    draftM);
            }

            stations.Add(new HullStationDto
            {
                Position = stationPos,
                Offsets = offsets,
                HasBulb = hasBulb,
                BulbOffsets = bulbOffsets,
            });
        }

        return Task.FromResult(new HullSectionsDto
        {
            Stations = stations,
            StationPositions = stationPositions,
        });
    }

    public Task<HullMesh3DDto> GenerateMeshAsync(
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        int longitudinalSegments = 60,
        int verticalSegments = 40,
        CancellationToken cancellationToken = default)
    {
        // Generate sections first
        var sections = GenerateSectionsAsync(
            shipdVector,
            lppM,
            beamM,
            draftM,
            metadata,
            longitudinalSegments,
            cancellationToken).Result;

        // Convert sections to mesh
        var vertices = new List<List<decimal>>();
        var faces = new List<List<int>>();
        var normals = new List<List<decimal>>();

        // Generate mesh vertices from sections
        foreach (var station in sections.Stations)
        {
            var x = station.Position * lppM; // Longitudinal position

            foreach (var (height, halfBreadth) in station.Offsets)
            {
                // Port side
                vertices.Add(new List<decimal> { x, -halfBreadth, height });
                // Starboard side
                vertices.Add(new List<decimal> { x, halfBreadth, height });

                // Calculate normal (simplified - pointing outward)
                var normalX = 0m;
                var normalY = halfBreadth > 0 ? 1m : 0m;
                var normalZ = 0m;
                normals.Add(new List<decimal> { normalX, normalY, normalZ });
                normals.Add(new List<decimal> { normalX, -normalY, normalZ });
            }

            // Add bulb vertices if present
            if (station.HasBulb && station.BulbOffsets != null)
            {
                foreach (var (height, halfBreadth) in station.BulbOffsets)
                {
                    vertices.Add(new List<decimal> { x, -halfBreadth, height });
                    vertices.Add(new List<decimal> { x, halfBreadth, height });
                    normals.Add(new List<decimal> { 0m, 1m, 0m });
                    normals.Add(new List<decimal> { 0m, -1m, 0m });
                }
            }
        }

        // Generate triangular faces (simplified - connect adjacent stations and heights)
        // This is a basic implementation; a more sophisticated approach would use proper triangulation
        for (int i = 0; i < sections.Stations.Count - 1; i++)
        {
            var station1 = sections.Stations[i];
            var station2 = sections.Stations[i + 1];
            var offsets1 = station1.Offsets.OrderBy(kvp => kvp.Key).ToList();
            var offsets2 = station2.Offsets.OrderBy(kvp => kvp.Key).ToList();

            // Create quads between stations (split into two triangles)
            for (int j = 0; j < Math.Min(offsets1.Count, offsets2.Count) - 1; j++)
            {
                // Quad vertices (simplified indexing - actual implementation needs proper vertex indexing)
                // This is a placeholder - proper mesh generation requires careful vertex indexing
            }
        }

        return Task.FromResult(new HullMesh3DDto
        {
            Vertices = vertices,
            Faces = faces,
            Normals = normals,
        });
    }

    public Task<ShipDValidationResultDto> ValidateParametersAsync(
        decimal[] shipdVector,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (shipdVector == null || shipdVector.Length != 45)
        {
            errors.Add("ShipD vector must contain exactly 45 parameters");
            return Task.FromResult(new ShipDValidationResultDto
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings,
            });
        }

        // Validate parameter ranges
        for (int i = 0; i < shipdVector.Length && i < metadata.Count; i++)
        {
            var param = metadata.FirstOrDefault(m => m.ParameterIndex == i);
            if (param == null) continue;

            var value = shipdVector[i];

            // Check if value is within min/max bounds
            if (param.Min.HasValue && value < param.Min.Value)
            {
                warnings.Add($"Parameter {param.Label} (index {i}) value {value} is below minimum {param.Min.Value}");
            }

            if (param.Max.HasValue && value > param.Max.Value)
            {
                warnings.Add($"Parameter {param.Label} (index {i}) value {value} is above maximum {param.Max.Value}");
            }
        }

        // Validate longitudinal proportions (Lb + Ls < 1.0)
        var lb = shipdVector[1];
        var ls = shipdVector[2];
        if (lb + ls >= 1.0m)
        {
            errors.Add($"Longitudinal proportions invalid: Lb ({lb}) + Ls ({ls}) must be < 1.0");
        }

        // Validate bulb parameters (only if bulb is enabled)
        if (shipdVector[31] > 0.5m) // bit_BB
        {
            var lbb = shipdVector[33];
            var hbb = shipdVector[34];
            var bbb = shipdVector[35];

            if (lbb <= 0 || hbb <= 0 || bbb <= 0)
            {
                warnings.Add("Bulb is enabled but bulb dimensions are zero or negative");
            }
        }

        return Task.FromResult(new ShipDValidationResultDto
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
        });
    }

    /// <summary>
    /// Denormalizes ShipD parameters from 0-1 range to physical units
    /// </summary>
    private Dictionary<int, decimal> DenormalizeParameters(
        decimal[] shipdVector,
        IReadOnlyList<ShipDParameterMetadataDto> metadata)
    {
        var denormalized = new Dictionary<int, decimal>();

        for (int i = 0; i < shipdVector.Length; i++)
        {
            var param = metadata.FirstOrDefault(m => m.ParameterIndex == i);
            var normalized = shipdVector[i];

            if (param != null && param.Min.HasValue && param.Max.HasValue)
            {
                // Linear denormalization: physical = min + (max - min) * normalized
                var physical = param.Min.Value + (param.Max.Value - param.Min.Value) * normalized;
                denormalized[i] = physical;
            }
            else
            {
                // No metadata - use normalized value as-is
                denormalized[i] = normalized;
            }
        }

        return denormalized;
    }

    /// <summary>
    /// Generates offsets for a single station based on ShipD parameters
    /// </summary>
    private Dictionary<decimal, decimal> GenerateStationOffsets(
        decimal stationPos,
        string region,
        Dictionary<int, decimal> denormalized,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata)
    {
        var offsets = new Dictionary<decimal, decimal>();

        // Always start with keel point (height=0, half-breadth=0) to close the bottom
        offsets[0m] = 0m;

        // Generate offsets at various heights
        var heightSteps = 20; // Number of height points
        for (int h = 1; h <= heightSteps; h++) // Start from 1 to avoid duplicate keel point
        {
            var height = (decimal)h / heightSteps * draftM; // Height from keel

            // Calculate half-breadth based on region and ShipD parameters
            decimal halfBreadth = 0m;

            if (region == "bow")
            {
                // Bow section: use Beta (flare), Cdrft (deadrise), Rc, Rk
                var beta = denormalized[8]; // Flare angle (degrees)
                var cdrft = denormalized[19]; // Deadrise angle (degrees)
                var rc = denormalized[9]; // Curvature coefficient
                var rk = denormalized[10]; // Knuckle coefficient

                // Simplified bow shape: combine flare and deadrise
                var heightRatio = height / draftM;
                var flareEffect = (decimal)Math.Tan((double)(beta * (decimal)Math.PI / 180m)) * height;
                var deadriseEffect = (decimal)Math.Tan((double)(cdrft * (decimal)Math.PI / 180m)) * (draftM - height);

                // Base half-breadth at this height (simplified - uses midship beam as reference)
                var baseHalfBreadth = beamM / 2m * (1m - heightRatio * 0.3m); // Taper toward keel
                halfBreadth = baseHalfBreadth + flareEffect * 0.1m; // Add flare effect
            }
            else if (region == "midship")
            {
                // Midship section: use bit_EP_S (sheer), bit_EP_T (tumblehome)
                var bitEPS = denormalized[20] > 0.5m; // Sheer extrusion
                var bitEPT = denormalized[21] > 0.5m; // Tumblehome

                var heightRatio = height / draftM;
                var baseHalfBreadth = beamM / 2m * (1m - heightRatio * 0.2m);

                if (bitEPT)
                {
                    // Tumblehome: inward curving upper sides
                    var tumblehomeFactor = heightRatio > 0.7m ? (heightRatio - 0.7m) / 0.3m : 0m;
                    baseHalfBreadth *= (1m - tumblehomeFactor * 0.15m);
                }

                halfBreadth = baseHalfBreadth;
            }
            else // stern
            {
                // Stern section: use Atrans, Beta_trans, Bc_trans, Rc_trans, Rk_trans
                var atrans = denormalized[22];
                var betaTrans = denormalized[27];
                var bcTrans = denormalized[28];
                var rcTrans = denormalized[29];
                var rkTrans = denormalized[30];

                var heightRatio = height / draftM;
                var baseHalfBreadth = beamM / 2m * (1m - heightRatio * 0.25m);

                // Apply transom effects
                if (stationPos < 0.1m) // Near transom
                {
                    var transomWidth = beamM * (decimal)bcTrans;
                    halfBreadth = Math.Min(baseHalfBreadth, transomWidth / 2m);
                }
                else
                {
                    halfBreadth = baseHalfBreadth;
                }
            }

            offsets[height] = Math.Max(0m, halfBreadth);
        }

        // Ensure we have a point at the deck level (draftM) to close the top
        if (!offsets.ContainsKey(draftM))
        {
            var maxHeight = offsets.Keys.Max();
            var maxHalfBreadth = offsets[maxHeight];
            offsets[draftM] = maxHalfBreadth * 0.95m; // Slightly narrower at deck
        }

        return offsets;
    }

    /// <summary>
    /// Generates bulb offsets for a station in the bow region
    /// </summary>
    private Dictionary<decimal, decimal> GenerateBulbOffsets(
        decimal stationPos,
        Dictionary<int, decimal> denormalized,
        decimal lppM,
        decimal beamM,
        decimal draftM)
    {
        var offsets = new Dictionary<decimal, decimal>();

        // Bulb parameters
        var lbb = denormalized[33]; // Bulb length ratio
        var hbb = denormalized[34]; // Bulb height ratio
        var bbb = denormalized[35]; // Bulb width ratio
        var lbbm = denormalized[36]; // Bulb asymmetry
        var rbb = denormalized[37]; // Bulb radius

        // Bulb is only in the forwardmost part of bow
        var bowStart = 1.0m - denormalized[1]; // Start of bow region
        if (stationPos < bowStart + lbb * 0.5m)
        {
            // Generate bulb shape (simplified - ellipsoid)
            var bulbLength = lbb * lppM;
            var bulbHeight = hbb * draftM;
            var bulbWidth = bbb * beamM;

            var heightSteps = 10;
            for (int h = 0; h <= heightSteps; h++)
            {
                var height = (decimal)h / heightSteps * bulbHeight;
                var heightRatio = height / bulbHeight;

                // Ellipsoid half-breadth
                var halfBreadth = bulbWidth / 2m * (decimal)Math.Sqrt(1.0 - (double)(heightRatio * heightRatio));
                offsets[height] = halfBreadth;
            }
        }

        return offsets;
    }
}
