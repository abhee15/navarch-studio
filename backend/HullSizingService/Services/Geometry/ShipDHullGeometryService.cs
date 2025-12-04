using System.Collections.Generic;
using System.Linq;
using HullSizingService.Services.Geometry;
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

        // Use draft directly from solver - it already accounts for displacement
        // The solver calculated this draft to match the candidate's target displacement
        // Previous code had hardcoded 13,500t target which was for an old 10,000 DWT test case
        // This caused extreme draft adjustments (e.g., 13.34m → 0.0036m) that crashed NURBS generation
        _logger.LogDebug(
            "[SHIPD_GEOMETRY] Using draft from solver: {Draft}m (no post-adjustment needed)",
            draftM);

        // CRITICAL: Enforce sensible curvature parameters before geometry generation
        // This prevents unfair hulls caused by zero or extreme curvature values
        EnsureSensibleCurvatureParameters(shipdVector, metadata);

        // Denormalize key parameters
        var denormalized = DenormalizeParameters(shipdVector, metadata);

        // Extract longitudinal proportions
        // CRITICAL: Lb and Ls are already ratios (0-1), use vector directly for region boundaries
        // Denormalized values might be in different ranges, but for region calculation we need ratios
        var lb = shipdVector[1]; // Bow length ratio (normalized 0-1)
        var ls = shipdVector[2]; // Stern length ratio (normalized 0-1)
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
                shipdVector,
                lppM,
                beamM,
                draftM,
                metadata);

            // Check for bulb (only in bow region)
            // CRITICAL: bit flags should use normalized vector directly, not denormalized
            var hasBulb = region == "bow" && shipdVector[31] > 0.5m; // bit_BB
            Dictionary<decimal, decimal>? bulbOffsets = null;

            if (hasBulb)
            {
                bulbOffsets = GenerateBulbOffsets(
                    stationPos,
                    denormalized,
                    shipdVector,
                    lppM,
                    beamM,
                    draftM);
            }

            // Check for skeg (only in stern region)
            // CRITICAL: bit flags should use normalized vector directly, not denormalized
            var hasSkeg = region == "stern" && shipdVector[32] > 0.5m; // bit_SB
            Dictionary<decimal, decimal>? skegOffsets = null;

            if (hasSkeg)
            {
                skegOffsets = GenerateSkegOffsets(
                    stationPos,
                    denormalized,
                    shipdVector,
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
                HasSkeg = hasSkeg,
                SkegOffsets = skegOffsets,
            });
        }

        // POST-PROCESSING: Ensure proper bow and stern closure
        // This corrects any artifacts in the generated geometry to ensure smooth, realistic hull shapes
        // Similar to ParentHullScaler corrections, but adapted for ShipD's dictionary-based offsets
        // CRITICAL: Pass shipdVector and denormalized to respect user-selected bow/stern families
        EnsureBowAndSternClosure(stations, beamM, draftM, shipdVector, denormalized);

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

            // Add skeg vertices if present
            // Skeg may extend below keel (negative heights)
            if (station.HasSkeg && station.SkegOffsets != null)
            {
                foreach (var (height, halfBreadth) in station.SkegOffsets)
                {
                    // At keel (height=0 or negative), port and starboard may share the same vertex
                    if (height <= 0 || halfBreadth == 0)
                    {
                        vertices.Add(new List<decimal> { x, 0m, height }); // Centerline vertex
                        normals.Add(new List<decimal> { 0m, 0m, 1m }); // Normal pointing up
                    }
                    else
                    {
                        vertices.Add(new List<decimal> { x, -halfBreadth, height }); // Port side
                        vertices.Add(new List<decimal> { x, halfBreadth, height }); // Starboard side
                        normals.Add(new List<decimal> { 0m, -1m, 0m }); // Normal pointing port
                        normals.Add(new List<decimal> { 0m, 1m, 0m }); // Normal pointing starboard
                    }
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

        // Validate skeg parameters (only if skeg is enabled)
        if (shipdVector[32] > 0.5m) // bit_SB
        {
            // Denormalize skeg parameters for validation
            var denormalized = DenormalizeParameters(shipdVector, metadata);
            var lsb = denormalized[39]; // Lsb - skeg length ratio
            var hsb = denormalized[41]; // Hsb - skeg height ratio (twin skeg)
            var bsb = denormalized[42]; // Bsb - skeg breadth ratio
            var hsboa = denormalized[40]; // HSBOA - skeg height to breadth ratio (single skeg)

            if (lsb <= 0 || bsb <= 0)
            {
                warnings.Add("Skeg is enabled but skeg length or breadth is zero or negative");
            }

            var isTwinSkeg = shipdVector[32] > 0.5m;
            if (isTwinSkeg && hsb <= 0)
            {
                warnings.Add("Twin skeg is enabled but skeg height (Hsb) is zero or negative");
            }
            else if (!isTwinSkeg && hsboa <= 0)
            {
                warnings.Add("Single skeg is enabled but skeg height-to-breadth ratio (HSBOA) is zero or negative");
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
    /// Estimates block coefficient (C_B) from ShipD parameters
    /// Uses form parameters to estimate hull fullness
    /// Typical range: 0.55 (fine) to 0.85 (full) for commercial vessels
    /// </summary>
    private decimal EstimateBlockCoefficient(
        decimal[] shipdVector,
        IReadOnlyList<ShipDParameterMetadataDto> metadata)
    {
        // Base C_B estimate: 0.65 (moderate fullness)
        decimal cb = 0.65m;

        // Adjust based on longitudinal proportions
        // Fuller ends (shorter bow/stern) → higher C_B
        var lb = shipdVector[1]; // Bow length ratio
        var ls = shipdVector[2]; // Stern length ratio
        var midBodyRatio = 1.0m - lb - ls;

        // More mid-body (fuller) → higher C_B
        cb += (midBodyRatio - 0.5m) * 0.15m; // ±0.075 adjustment

        // Adjust based on curvature parameters
        // Lower curvature (fuller sections) → higher C_B
        var rc = shipdVector[9]; // Bow curvature (normalized 0-1)
        var rcTrans = shipdVector[29]; // Stern curvature (normalized 0-1)
        var avgCurvature = (rc + rcTrans) / 2m;

        // Lower curvature = fuller = higher C_B
        cb += (1m - avgCurvature) * 0.1m; // Up to 0.1 adjustment

        // Clamp to reasonable range
        cb = Math.Clamp(cb, 0.55m, 0.85m);

        return cb;
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
    /// Enhanced to match frontend implementation with longitudinal scaling and improved vertical profiles
    /// </summary>
    private Dictionary<decimal, decimal> GenerateStationOffsets(
        decimal stationPos,
        string region,
        Dictionary<int, decimal> denormalized,
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata)
    {
        var offsets = new Dictionary<decimal, decimal>();

        // === DEFENSIVE VALIDATION ===
        // Validate all critical inputs before ANY calculations to prevent exceptions
        // Note: decimal type in C# cannot be NaN/Infinity (would throw on creation), but can be invalid/negative
        if (beamM <= 0m)
        {
            _logger.LogError("[SHIPD_GEOMETRY] Invalid beam: {Beam}. Returning minimal offsets.", beamM);
            offsets[0m] = 0m;
            return offsets;
        }
        if (draftM <= 0m)
        {
            _logger.LogError("[SHIPD_GEOMETRY] Invalid draft: {Draft}. Returning minimal offsets.", draftM);
            offsets[0m] = 0m;
            return offsets;
        }
        if (stationPos < 0m || stationPos > 1m)
        {
            _logger.LogError("[SHIPD_GEOMETRY] Invalid station position: {StationPos}. Returning minimal offsets.", stationPos);
            offsets[0m] = 0m;
            return offsets;
        }

        // Always start with keel point (height=0, half-breadth=0) to close the bottom
        offsets[0m] = 0m;

        // Generate offsets at various heights (including freeboard above waterline)
        var heightSteps = 20; // Number of height points
        var maxHeight = draftM * 1.35m; // Include 35% freeboard above waterline

        for (int h = 1; h <= heightSteps; h++) // Start from 1 to avoid duplicate keel point
        {
            var height = (decimal)h / heightSteps * maxHeight; // Height from keel to above waterline

            // Calculate half-breadth based on region and ShipD parameters
            decimal halfBreadth = 0m;

            if (region == "bow")
            {
                // Bow section: use Beta (flare), Cdrft (deadrise), Rc, Rk, Kappa_bow
                var beta = denormalized[8]; // Flare angle (degrees)

                // PHASE 2: Enforce R_c (curvature) parameter: 0.2-0.4 normalized for smooth bilge radius
                // R_c defines smooth radius connecting flat bottom to vertical side
                // Fix: Increase from 0.000 to 0.2-0.4 to ensure bow sections have smooth, continuous curves
                var rcNorm = shipdVector[9]; // Curvature coefficient (normalized 0-1)
                // Enforce minimum threshold: values < 0.05 are too small and create hard chines
                var rc = rcNorm < 0.05m ? 0.3m : Math.Clamp(rcNorm, 0.2m, 0.4m); // Default 0.3 if too small

                // PHASE 2: Enforce R_k (knuckle) parameter: 0.1-0.3 normalized for deck edge transition
                // R_k defines smooth connection between vertical side and deck/sheer line
                // Fix: Set to moderate positive value (0.1-0.3) to introduce gentle radius and ensure continuity
                var rkNorm = shipdVector[10]; // Knuckle coefficient (normalized 0-1)
                // Enforce minimum threshold: values < 0.05 or negative/extreme negative create sharp transitions
                var rk = (rkNorm < 0.05m || rkNorm < -0.5m) ? 0.2m : Math.Clamp(rkNorm, 0.1m, 0.3m); // Default 0.2 if too small/extreme

                // Enforce Kappa_bow: should be around 0.4-0.6 for smooth sections (not 0.000)
                var kappaBowNorm = shipdVector[14];
                var kappaBow = denormalized[14]; // Curvature type (-1 to 1)
                // If normalized value is too close to zero, adjust denormalized to neutral (0.0 = 0.5 normalized)
                if (Math.Abs(kappaBowNorm) < 0.1m)
                {
                    kappaBow = 0.0m; // Neutral curvature (0.5 normalized)
                }
                var cdrft = denormalized[19]; // Deadrise angle (degrees)

                var heightRatio = height / draftM;

                if (height <= draftM)
                {
                    // BELOW WATERLINE: Expand from narrow keel to wide waterline
                    // Calculate keel width (reduced by deadrise)
                    var deadriseReduction = (decimal)Math.Tan((double)(cdrft * (decimal)Math.PI / 180m)) * (draftM - height);
                    var keelHalfBreadth = Math.Max(0m, (beamM / 2m) * 0.1m); // Keel is ~10% of beam

                    // Expansion curve from keel to waterline
                    // Higher Rc = fuller (straighter expansion), Lower Rc = finer (more curved)
                    var curvePower = 2.5m - rc * 1.5m; // Range: 1.0 (full) to 2.5 (fine)
                    var expansionRatio = (decimal)Math.Pow((double)heightRatio, (double)(1m / curvePower));

                    // Interpolate from keel to max beam
                    var baseHalfBreadth = keelHalfBreadth +
                        (beamM / 2m - keelHalfBreadth - deadriseReduction * 0.3m) * expansionRatio;

                    // PHASE 2: Apply knuckle effect with proper R_k radius (0.1-0.3)
                    // R_k now always has a valid value (enforced above), so always apply
                    var knuckleHeight = 0.5m; // Knuckle at mid-height
                    var knuckleRange = 0.2m;
                    if (Math.Abs(heightRatio - knuckleHeight) < knuckleRange)
                    {
                        var knuckleFactor = 1m - Math.Abs(heightRatio - knuckleHeight) / knuckleRange;
                        // Scale knuckle effect by R_k value (0.1-0.3 range, normalized to 0-1)
                        var knuckleEffect = (rk - 0.1m) / 0.2m; // Normalize 0.1-0.3 to 0-1
                        baseHalfBreadth *= 1m + knuckleEffect * knuckleFactor * 0.15m;
                    }

                    // Apply convex/concave control
                    if (Math.Abs(kappaBow - 0.5m) > 0.1m)
                    {
                        var convexEffect = (kappaBow - 0.5m) * 2m * (decimal)Math.Sin((double)(heightRatio * (decimal)Math.PI / 2m)) * 0.1m;
                        baseHalfBreadth *= 1m + convexEffect;
                    }

                    halfBreadth = Math.Max(0m, baseHalfBreadth);
                }
                else
                {
                    // ABOVE WATERLINE: Apply flare (widen)
                    var aboveWLHeight = height - draftM;
                    var baseHalfBreadth = beamM / 2m;

                    // Add flare effect
                    if (beta > 5m)
                    {
                        var flareExpansion = (decimal)Math.Tan((double)(beta * (decimal)Math.PI / 180m)) * aboveWLHeight;
                        baseHalfBreadth += flareExpansion * 0.2m;
                    }

                    halfBreadth = Math.Max(0m, baseHalfBreadth);
                }
            }
            else if (region == "midship")
            {
                // Midship section: Check for deep_v_midship (yacht) vs standard midship
                // Deep V midship uses: Adrft (17), Bdrft (18), Cdrft (19)
                // Standard midship uses: bit_EP_S (20), bit_EP_T (21)

                // PARAMETER UPDATE: Deadrise (Cdrft) - Ensure 5°-8° for standard midship
                // Deadrise reduces GM and improves roll period by lifting keel slightly
                // Problem: Flat bottom (0.011 deg ≈ 0.0%) causes poor roll characteristics
                // Solution: Increase to 5°-8° to introduce V-shape and reduce initial stability
                var cdrftRaw = denormalized[19]; // Deadrise angle (degrees)
                var cdrft = cdrftRaw <= 0.1m ? 6.5m : Math.Clamp(cdrftRaw, 5m, 8m); // Default 6.5° if ≤0.1° (was 0.011°)
                var isDeepV = cdrft > 20m; // Deep V midship typically has higher deadrise

                // PARAMETER UPDATE: Curvature (Rc) Midship - Add smooth bilge radius
                // Problem: Hard bilge corner (Rc=0.000) causes poor flow and contributes to stiffness
                // Solution: Increase to 0.3-0.5 to create smooth radius at bilge, improving flow
                // Use bow curvature parameter (Rc, index 9) for midship bilge curvature
                var rcRaw = denormalized[9]; // Curvature coefficient (reuse bow Rc for midship)
                var rcMidship = rcRaw <= 0m ? 0.4m : Math.Clamp(rcRaw, 0.3m, 0.5m); // Default 0.4 if zero/negative

                var heightRatio = height / draftM;

                if (height <= draftM)
                {
                    // BELOW WATERLINE
                    if (isDeepV)
                    {
                        // DEEP V MIDSHIP (yacht): Use Adrft, Bdrft, Cdrft for deadrise control
                        var adrft = denormalized[17]; // Draft rocker coefficient A
                        var bdrft = denormalized[18]; // Draft rocker coefficient B

                        // Deep V has more deadrise (narrower keel, wider at waterline)
                        // Cdrft controls the deadrise angle directly
                        var deadriseAngle = cdrft; // Already in degrees
                        var deadriseReduction = (decimal)Math.Tan((double)(deadriseAngle * (decimal)Math.PI / 180m)) * (draftM - height);

                        // Keel width is narrower for deep V (more deadrise)
                        var keelHalfBreadth = Math.Max(0m, (beamM / 2m) * (0.05m + adrft * 0.05m)); // 5-10% of beam, modulated by Adrft

                        // Expansion curve with deadrise effect
                        // Bdrft affects the curve shape
                        var curvePower = 1.5m - bdrft * 0.5m; // Range: 1.0-1.5 (more V-shaped)
                        var expansionRatio = (decimal)Math.Pow((double)heightRatio, (double)(1m / curvePower));

                        var baseHalfBreadth = keelHalfBreadth +
                            (beamM / 2m - keelHalfBreadth - deadriseReduction * 0.2m) * expansionRatio;

                        halfBreadth = Math.Max(0m, baseHalfBreadth);
                    }
                    else
                    {
                        // STANDARD MIDSHIP with deadrise (5°-8°) and curvature (Rc 0.3-0.5)
                        // Apply deadrise to lift keel slightly, reducing waterplane area near center
                        // This reduces GM and improves roll period (softer, more comfortable)
                        var deadriseReduction = (decimal)Math.Tan((double)(cdrft * (decimal)Math.PI / 180m)) * (draftM - height);

                        // Keel width is narrower due to deadrise (V-shape)
                        // Deadrise creates a slight V, so keel is narrower than without deadrise
                        var keelHalfBreadth = Math.Max(0m, (beamM / 2m) * (0.15m - cdrft / 100m)); // 10-15% of beam, modulated by deadrise

                        // PARAMETER UPDATE: Apply curvature (Rc) for smooth bilge radius
                        // Higher Rc = smoother transition (fuller), Lower Rc = sharper (finer)
                        // Rc 0.3-0.5 creates smooth radius at bilge, improving flow
                        var curvePower = 2.5m - rcMidship * 1.5m; // Range: 1.0 (full/smooth) to 2.5 (fine/sharp)
                        var expansionRatio = (decimal)Math.Pow((double)heightRatio, (double)(1m / curvePower));

                        // Apply bilge curvature effect - smooth transition at mid-height (bilge region)
                        var bilgeHeight = 0.3m; // Bilge typically at ~30% of draft
                        var bilgeRange = 0.2m; // Smooth transition range
                        if (Math.Abs(heightRatio - bilgeHeight) < bilgeRange)
                        {
                            var bilgeFactor = 1m - Math.Abs(heightRatio - bilgeHeight) / bilgeRange;
                            // Higher Rc (0.3-0.5) creates smoother, more rounded bilge
                            var curvatureEffect = (rcMidship - 0.3m) / 0.2m; // Normalize 0.3-0.5 to 0-1
                            expansionRatio *= 1m + curvatureEffect * bilgeFactor * 0.1m; // Smooth out bilge transition
                        }

                        var baseHalfBreadth = keelHalfBreadth +
                            (beamM / 2m - keelHalfBreadth - deadriseReduction * 0.3m) * expansionRatio;

                        halfBreadth = Math.Max(0m, baseHalfBreadth);
                    }
                }
                else
                {
                    // ABOVE WATERLINE
                    var aboveWLHeight = height - draftM;
                    var freeboard = draftM * 0.35m; // Match total freeboard (35%)
                    var aboveWLRatio = Math.Min(aboveWLHeight / freeboard, 1.0m);

                    var baseHalfBreadth = beamM / 2m;

                    if (isDeepV)
                    {
                        // Deep V midship: Use Adrft/Bdrft for sheer effects above waterline
                        var adrft = denormalized[17];
                        var bdrft = denormalized[18];

                        // Adrft affects outward curve (sheer)
                        if (adrft > 0 && aboveWLRatio > 0.5m)
                        {
                            baseHalfBreadth *= 1m + adrft * ((aboveWLRatio - 0.5m) / 0.5m) * 0.05m;
                        }

                        // Bdrft affects inward curve (tumblehome)
                        if (bdrft < 0 && aboveWLRatio > 0.5m)
                        {
                            baseHalfBreadth *= 1m + bdrft * ((aboveWLRatio - 0.5m) / 0.5m) * 0.05m;
                        }
                    }
                    else
                    {
                        // STANDARD MIDSHIP: Use bit_EP_S (sheer), bit_EP_T (tumblehome)
                        var bitEPS = denormalized[20] > 0.5m; // Sheer extrusion
                        var bitEPT = denormalized[21] > 0.5m; // Tumblehome

                        // Sheer (outward curve at deck)
                        if (bitEPS && aboveWLRatio > 0.6m)
                        {
                            baseHalfBreadth *= 1m + ((aboveWLRatio - 0.6m) / 0.4m) * 0.08m;
                        }

                        // Tumblehome (inward curve at deck)
                        if (bitEPT && aboveWLRatio > 0.5m)
                        {
                            baseHalfBreadth *= 1m - ((aboveWLRatio - 0.5m) / 0.5m) * 0.15m;
                        }
                    }

                    halfBreadth = baseHalfBreadth;
                }
            }
            else // stern
            {
                // Stern section: Detect transom_stern vs canoe_stern from Atrans parameter
                // Transom stern uses: Atrans, Beta_trans, Bc_trans, Rc_trans, Rk_trans, Kappa_stern
                // Canoe stern uses: Adel_stern (25), Bdel_stern (26)

                // CRITICAL: Use normalized vector for family detection (consistent with longitudinal scaling)
                var atransNorm = shipdVector[22]; // Transom area coefficient (normalized 0-1)
                var isTransomStern = atransNorm > 0.5m; // Transom when > 0.5, canoe when <= 0.5

                // Enforce Kappa_stern: should be around 0.4-0.6 for smooth sections (not 0.000)
                var kappaSternNorm = shipdVector[24];
                var kappaStern = denormalized[24]; // Curvature type
                // If normalized value is too close to zero, adjust denormalized to neutral (0.0 = 0.5 normalized)
                if (Math.Abs(kappaSternNorm) < 0.1m)
                {
                    kappaStern = 0.0m; // Neutral curvature (0.5 normalized)
                }
                var betaTrans = denormalized[27];
                var bcTrans = denormalized[28];

                // PHASE 2: Enforce R_c (curvature) for stern: 0.2-0.4 normalized
                // Fix: Increase from 0.000 to 0.2-0.4 to round the transition from side to transom face
                var rcTransNorm = shipdVector[29]; // Stern curvature coefficient (normalized 0-1)
                // Enforce minimum threshold: values < 0.05 are too small and create hard chines
                var rcTrans = rcTransNorm < 0.05m ? 0.3m : Math.Clamp(rcTransNorm, 0.2m, 0.4m); // Default 0.3 if too small

                // PHASE 2: Enforce R_k (knuckle) for stern: 0.0-0.2 normalized
                // Fix: Set to 0.0 or slightly positive to define sheer line knuckle smoothly
                var rkTransNorm = shipdVector[30]; // Stern knuckle coefficient (normalized 0-1)
                // Enforce minimum threshold: values < 0.02 or negative/extreme negative create sharp transitions
                var rkTrans = (rkTransNorm < 0.02m || rkTransNorm < -0.5m) ? 0.1m : Math.Clamp(rkTransNorm, 0.0m, 0.2m); // Default 0.1 if too small/extreme
                var adelStern = denormalized[25]; // Canoe stern sheer coefficient A
                var bdelStern = denormalized[26]; // Canoe stern sheer coefficient B

                var heightRatio = height / draftM;

                if (height <= draftM)
                {
                    // BELOW WATERLINE: Expand from narrow keel to wide waterline
                    var keelHalfBreadth = (beamM / 2m) * 0.15m; // Stern keel slightly wider than bow

                    if (isTransomStern)
                    {
                        // TRANSOM STERN: Use transom parameters
                        var atrans = denormalized[22];

                        // Curvature expansion
                        var curvePower = 2.5m - rcTrans * 1.5m; // Range: 1.0 (full) to 2.5 (fine)
                        var expansionRatio = (decimal)Math.Pow((double)heightRatio, (double)(1m / curvePower));

                        var baseHalfBreadth = keelHalfBreadth + (beamM / 2m - keelHalfBreadth) * expansionRatio;

                        // Transom effect (flat stern) - only near the very aft
                        if (stationPos < 0.15m && atrans > 0.5m)
                        {
                            var transomWidth = beamM * bcTrans;
                            var transomBlend = (0.15m - stationPos) / 0.15m; // Blend over aft 15%
                            baseHalfBreadth = baseHalfBreadth * (1m - transomBlend * atrans) +
                                (transomWidth / 2m) * transomBlend * atrans;
                        }

                        // PHASE 2: Apply stern knuckle with proper R_k radius (0.0-0.2)
                        // R_k now always has a valid value, so always apply if in range
                        if (heightRatio > 0.3m && heightRatio < 0.6m)
                        {
                            var knuckleFactor = 1m - Math.Abs(heightRatio - 0.45m) / 0.15m;
                            // Scale knuckle effect by R_k value (0.0-0.2 range, normalized to 0-1)
                            var knuckleEffect = (rkTrans - 0.0m) / 0.2m; // Normalize 0.0-0.2 to 0-1
                            baseHalfBreadth *= 1m + knuckleEffect * knuckleFactor * 0.12m;
                        }

                        // Apply convex/concave control
                        if (Math.Abs(kappaStern - 0.5m) > 0.1m)
                        {
                            var convexEffect = (kappaStern - 0.5m) * 2m * (decimal)Math.Sin((double)(heightRatio * (decimal)Math.PI / 2m)) * 0.1m;
                            baseHalfBreadth *= 1m + convexEffect;
                        }

                        halfBreadth = Math.Max(0m, baseHalfBreadth);
                    }
                    else
                    {
                        // CANOE STERN (yacht): Use Adel_stern, Bdel_stern for rounded stern shape
                        // Canoe stern has more rounded, elliptical sections (less V-shaped)

                        // Curvature expansion - canoe stern is more rounded (higher power)
                        var curvePower = 2.0m + rcTrans * 1.0m; // Range: 2.0-3.0 (more rounded)
                        var expansionRatio = (decimal)Math.Pow((double)heightRatio, (double)(1m / curvePower));

                        var baseHalfBreadth = keelHalfBreadth + (beamM / 2m - keelHalfBreadth) * expansionRatio;

                        // Adel_stern and Bdel_stern affect the stern curvature
                        // Adel_stern: affects the vertical curvature (sheer)
                        // Bdel_stern: affects the horizontal curvature (roundness)
                        if (Math.Abs(adelStern) > 0.1m)
                        {
                            var adelEffect = adelStern * (decimal)Math.Sin((double)(heightRatio * (decimal)Math.PI)) * 0.1m;
                            baseHalfBreadth *= 1m + adelEffect;
                        }

                        if (Math.Abs(bdelStern) > 0.1m)
                        {
                            var bdelEffect = bdelStern * (decimal)Math.Cos((double)(heightRatio * (decimal)Math.PI / 2m)) * 0.08m;
                            baseHalfBreadth *= 1m + bdelEffect;
                        }

                        // Apply convex/concave control (less pronounced for canoe stern)
                        if (Math.Abs(kappaStern - 0.5m) > 0.1m)
                        {
                            var convexEffect = (kappaStern - 0.5m) * 2m * (decimal)Math.Sin((double)(heightRatio * (decimal)Math.PI / 2m)) * 0.05m; // Reduced effect
                            baseHalfBreadth *= 1m + convexEffect;
                        }

                        halfBreadth = Math.Max(0m, baseHalfBreadth);
                    }
                }
                else
                {
                    // ABOVE WATERLINE
                    var aboveWLHeight = height - draftM;
                    var baseHalfBreadth = beamM / 2m;

                    if (isTransomStern)
                    {
                        // TRANSOM STERN: Apply rake (aft overhang)
                        if (betaTrans > 5m && stationPos < 0.2m)
                        {
                            var rakeExpansion = (decimal)Math.Tan((double)(betaTrans * (decimal)Math.PI / 180m)) * aboveWLHeight;
                            baseHalfBreadth += rakeExpansion * 0.15m;
                        }
                    }
                    else
                    {
                        // CANOE STERN: Use Adel_stern/Bdel_stern for sheer effects above waterline
                        var freeboard = draftM * 0.35m;
                        var aboveWLRatio = Math.Min(aboveWLHeight / freeboard, 1.0m);

                        // Adel_stern affects outward curve (sheer) above waterline
                        if (adelStern > 0 && aboveWLRatio > 0.4m)
                        {
                            baseHalfBreadth *= 1m + adelStern * ((aboveWLRatio - 0.4m) / 0.6m) * 0.08m;
                        }

                        // Bdel_stern affects inward curve (tumblehome) above waterline
                        if (bdelStern < 0 && aboveWLRatio > 0.5m)
                        {
                            baseHalfBreadth *= 1m + bdelStern * ((aboveWLRatio - 0.5m) / 0.5m) * 0.1m;
                        }
                    }

                    halfBreadth = Math.Max(0m, baseHalfBreadth);
                }
            }

            // LONGITUDINAL SCALING: Apply taper based on station position, region, and hull family
            // This creates the actual bow/stern taper (hull narrows toward ends)
            // CRITICAL: Different stern families require different taper profiles to avoid incorrect V-shapes
            decimal longitudinalScale = 1.0m;

            if (region == "bow")
            {
                // Bow region: Taper from full beam at bowStart to centerline at bow tip (pos=1.0)
                // Use normalized vector values for ratios (Lb, Ls are already 0-1 ratios)
                var lb = shipdVector[1]; // Bow length ratio (normalized 0-1)
                var ls = shipdVector[2]; // Stern length ratio (normalized 0-1)
                var bowStart = 1.0m - lb - ls + ls; // Start of bow region

                if (stationPos >= bowStart)
                {
                    // Position within bow region: 0 = bow start (full beam), 1 = bow tip (centerline)
                    var bowPos = (stationPos - bowStart) / (1.0m - bowStart);

                    // Detect bow family from parameters
                    var hasBulb = shipdVector[31] > 0.5m; // bit_BB (normalized 0-1, >0.5 = enabled)
                    var beta = denormalized[8]; // Flare angle (degrees, denormalized)
                    var rc = denormalized[9]; // Curvature coefficient (denormalized)

                    if (hasBulb)
                    {
                        // Bulbous bow: More gradual, rounded taper (not sharp V)
                        // Use higher power for smoother curve
                        longitudinalScale = (decimal)Math.Pow((double)(1.0m - bowPos), 2.5);
                    }
                    else if (beta > 20m)
                    {
                        // High flare bow (wave piercing): More gradual taper
                        longitudinalScale = (decimal)Math.Pow((double)(1.0m - bowPos), 2.2);
                    }
                    else
                    {
                        // Standard bow: Quadratic taper
                        longitudinalScale = (decimal)Math.Pow((double)(1.0m - bowPos), 2.0);
                    }
                }
            }
            else if (region == "stern")
            {
                // Stern region: Taper from centerline at stern tip (pos=0.0) to full beam at midStart
                // Use normalized vector value for ratio (Ls is already 0-1 ratio)
                var ls = shipdVector[2]; // Stern length ratio (normalized 0-1)

                // CRITICAL: Detect stern family from Atrans parameter
                // Use normalized vector values (0-1 range) for family detection and calculations
                var atransNorm = shipdVector[22]; // Transom area coefficient (normalized 0-1)
                var bcTransNorm = shipdVector[28]; // Transom width ratio (normalized 0-1)
                var rcTransNorm = shipdVector[29]; // Stern curvature coefficient (normalized 0-1)

                if (stationPos <= ls)
                {
                    // Position within stern region: 0 = stern tip (centerline), 1 = stern end (full beam)
                    var sternPos = stationPos / ls;

                    // TRANSOM STERN: When Atrans (normalized) > 0.5, maintain full beam until very close to transom
                    // Then apply flat transom (not V-shape taper)
                    // Note: Atrans normalized 0-1: 0 = pointed stern, 1 = full transom
                    if (atransNorm > 0.5m)
                    {
                        // Transom stern: Maintain full beam until last 5-10% of stern
                        // Then transition to transom width (flat stern)
                        var transomStartPos = 0.90m; // Start transom at 90% of stern length

                        if (sternPos < transomStartPos)
                        {
                            // Before transom: Maintain full beam (no V-shape taper)
                            longitudinalScale = 1.0m;
                        }
                        else
                        {
                            // At transom: Transition to transom width
                            var transomBlend = (sternPos - transomStartPos) / (1.0m - transomStartPos);
                            // Transom width is typically 70-100% of beam (bcTransNorm controls this)
                            // bcTransNorm is normalized 0-1, map to 0.7-1.0 range
                            var transomWidthRatio = 0.7m + bcTransNorm * 0.3m; // 0.7 to 1.0
                            longitudinalScale = 1.0m - (1.0m - transomWidthRatio) * transomBlend;
                        }
                    }
                    else
                    {
                        // CRUISER/CANOE STERN: When Atrans < 0.5, use rounded/elliptical taper
                        // Higher Rc_trans = fuller (rounder), Lower Rc_trans = finer (more pointed)
                        // Use normalized vector value (0-1) for consistent exponent calculation
                        // rcTransNorm already declared in outer scope (line 544)
                        var curveExponent = 2.0m + rcTransNorm * 1.0m; // Range: 2.0 (standard) to 3.0 (very rounded)

                        // Use smoother, more gradual taper for cruiser/canoe sterns
                        // Elliptical taper: sternPos^exponent (instead of sternPos^2)
                        longitudinalScale = (decimal)Math.Pow((double)sternPos, (double)curveExponent);

                        // Ensure minimum scale to avoid sharp V-shape
                        if (longitudinalScale < 0.3m && sternPos > 0.3m)
                        {
                            // For cruiser/canoe sterns, maintain reasonable width even near tip
                            longitudinalScale = 0.3m + (sternPos - 0.3m) * 0.7m / 0.7m;
                        }
                    }
                }
            }
            // Midship region: longitudinalScale = 1.0 (no taper, full beam)

            // === VALIDATE LONGITUDINAL SCALE ===
            // Ensure longitudinalScale is valid and positive
            if (longitudinalScale < 0m || longitudinalScale > 2m)
            {
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Invalid longitudinal scale detected: {LongitudinalScale} at station {StationPos} in {Region}. Using fallback 1.0",
                    longitudinalScale, stationPos, region);
                longitudinalScale = 1.0m;
            }

            // Apply longitudinal scaling to half-breadth
            halfBreadth = halfBreadth * longitudinalScale;

            // === VALIDATE HALF-BREADTH ===
            // Ensure half-breadth is reasonable (catch any calculation errors)
            if (halfBreadth < 0m || halfBreadth > beamM)
            {
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Invalid half-breadth detected: {HalfBreadth} at height={Height}, station={StationPos}, region={Region}. Clamping to valid range.",
                    halfBreadth, height, stationPos, region);
                halfBreadth = Math.Clamp(halfBreadth, 0m, beamM / 2m);
            }

            // PARAMETER UPDATE: Waterline fining to reduce CWP from 0.800 to 0.75-0.78
            // Problem: CWP=0.800 is too high (WL parameter 0.050), contributing to excessive initial stability
            // Solution: Fine (narrow) the waterlines in the Plan View to reduce waterplane area
            // Target: reduce CWP from 0.800 to 0.75-0.78 (3-6% reduction in waterplane area)
            if (height >= draftM * 0.9m && height <= draftM * 1.1m) // Near waterline (90-110% of draft)
            {
                // Apply fining to all regions (bow, midship, stern) to reduce overall CWP
                // Use WL parameter (index 6) to control fining intensity
                // Lower WL value = more fining (narrower waterlines)
                var wlParam = shipdVector[6]; // WL - Waterline length ratio (normalized 0-1)

                // Base fining factor: 4% reduction (target CWP reduction from 0.800 to 0.77)
                // Adjust based on WL parameter: if WL is low (0.050), apply more fining
                var baseFiningFactor = 0.96m; // 4% reduction
                // Map WL (0.05-0.8 range) to fining adjustment
                // Lower WL = more fining needed to reduce CWP
                var wlNormalized = Math.Clamp((wlParam - 0.05m) / 0.75m, 0m, 1m); // Normalize WL to 0-1
                var wlAdjustment = (0.5m - wlNormalized) * 0.04m; // ±2% adjustment based on WL
                var finingFactor = baseFiningFactor + wlAdjustment; // Range: 0.94-0.98 (3-6% reduction)

                // Apply fining with smooth transition near waterline
                var heightRatio = height / draftM;
                var waterlineProximity = 1m - Math.Abs(heightRatio - 1.0m) / 0.1m; // 1.0 at waterline, 0.0 at ±10%
                var effectiveFining = 1m - (1m - finingFactor) * waterlineProximity;
                halfBreadth *= effectiveFining;
            }

            offsets[height] = Math.Max(0m, halfBreadth);
        }

        // Ensure we have a point at the deck level (draftM) to close the top
        if (!offsets.ContainsKey(draftM))
        {
            var currentMaxHeight = offsets.Keys.Max();
            var maxHalfBreadth = offsets[currentMaxHeight];
            offsets[draftM] = maxHalfBreadth * 0.95m; // Slightly narrower at deck
        }

        // PHASE 2: Apply NURBS fairing for C² continuity
        // Convert dictionary to sorted lists for NURBS processing
        var sortedHeights = offsets.Keys.OrderBy(h => h).ToList();
        var sortedHalfBreadths = sortedHeights.Select(h => offsets[h]).ToList();

        if (sortedHeights.Count >= 4) // Need at least 4 points for cubic NURBS
        {
            // Generate control points from existing offsets
            var controlPoints = sortedHeights.Zip(sortedHalfBreadths, (h, y) => (x: h, y: y)).ToList();

            // Determine boundary conditions based on region
            decimal? startSlope = null;
            decimal? endSlope = null;
            decimal? startCurvature = null;

            // Centerline: dy/dx = 0 (zero slope at keel)
            if (sortedHeights[0] == 0m)
            {
                startSlope = 0m;
            }

            // Midship: d²y/dx² = 0 (zero curvature for parallel mid-body)
            if (region == "midship")
            {
                startCurvature = 0m;
            }

            // Generate evaluation points in parameter space [0,1] normalized by height range
            var maxHeightForNurbs = sortedHeights.Last();
            var evaluationPoints = sortedHeights.Select(h => maxHeightForNurbs > 0 ? h / maxHeightForNurbs : 0m).ToList();

            // Apply NURBS fairing with boundary conditions
            // Note: NURBS expects control points with x in [0,1] parameter space
            var normalizedControlPoints = controlPoints.Select(cp =>
                (x: maxHeightForNurbs > 0 ? cp.x / maxHeightForNurbs : 0m, y: cp.y)).ToList();

            var fairedHalfBreadths = NurbsCurveGenerator.GenerateCurveWithBoundaryConditions(
                normalizedControlPoints,
                degree: 3,
                startSlope: startSlope,
                endSlope: endSlope,
                startCurvature: startCurvature,
                endCurvature: null,
                evaluationPoints: evaluationPoints);

            // Update offsets with faired values
            // CRITICAL: Validate faired values to prevent invalid values from NURBS evaluation
            for (int i = 0; i < sortedHeights.Count && i < fairedHalfBreadths.Count; i++)
            {
                var fairedValue = fairedHalfBreadths[i];
                // Validate faired value is in reasonable range
                if (fairedValue < 0m || fairedValue > beamM)
                {
                    _logger.LogWarning(
                        "[SHIPD_GEOMETRY] Invalid faired value detected at height={Height}, station={StationPos}: {Value}. Using original value.",
                        sortedHeights[i], stationPos, fairedValue);
                    // Keep original value instead of invalid value from NURBS
                    continue;
                }
                offsets[sortedHeights[i]] = Math.Max(0m, fairedValue);
            }
        }

        // === FINAL VALIDATION ===
        // Ensure no invalid values made it through to final offsets
        var invalidCount = 0;
        foreach (var kvp in offsets.ToList())
        {
            if (kvp.Value < 0m || kvp.Value > beamM)
            {
                _logger.LogError(
                    "[SHIPD_GEOMETRY] Invalid offset found at height={Height}: {Value}. Removing entry.",
                    kvp.Key, kvp.Value);
                offsets.Remove(kvp.Key);
                invalidCount++;
            }
        }
        if (invalidCount > 0)
        {
            _logger.LogError(
                "[SHIPD_GEOMETRY] ❌ Removed {InvalidCount} invalid offset values from station at position {StationPos}",
                invalidCount, stationPos);
        }

        return offsets;
    }

    /// <summary>
    /// Ensures proper bow and stern closure by correcting any artifacts in generated geometry
    /// This is a post-processing step that does NOT change offset generation, only corrects scaling artifacts
    /// CRITICAL: Respects user-selected bow/stern families (bulbous, wave piercing, transom, cruiser, canoe)
    /// </summary>
    private void EnsureBowAndSternClosure(
        List<HullStationDto> stations,
        decimal beamM,
        decimal draftM,
        decimal[] shipdVector,
        Dictionary<int, decimal> denormalized)
    {
        if (stations == null || stations.Count < 2)
        {
            return; // Need at least 2 stations for closure correction
        }

        // Ensure bow closure (last station, position closest to 1.0)
        // CRITICAL: Respect user-selected bow family (bulbous, wave piercing, standard)
        var bowStation = stations
            .Select((s, idx) => new { Station = s, Index = idx, Position = s.Position })
            .OrderByDescending(x => x.Position)
            .FirstOrDefault();

        if (bowStation != null && bowStation.Position > 0.95m) // Bow station (near position 1.0)
        {
            var bowOffsets = bowStation.Station.Offsets;
            if (bowOffsets != null && bowOffsets.Count > 0)
            {
                // Detect bow family from parameters to adjust closure logic
                var hasBulb = shipdVector[31] > 0.5m; // bit_BB
                var beta = denormalized.ContainsKey(8) ? denormalized[8] : 0m; // Flare angle

                // Find adjacent station (second-to-last)
                var adjacentStation = stations
                    .Select((s, idx) => new { Station = s, Index = idx, Position = s.Position })
                    .Where(x => x.Position < bowStation.Position)
                    .OrderByDescending(x => x.Position)
                    .FirstOrDefault();

                if (adjacentStation != null && adjacentStation.Station.Offsets != null)
                {
                    var adjacentOffsets = adjacentStation.Station.Offsets;
                    var maxBowHalfBreadth = bowOffsets.Values.Max();
                    var maxAdjacentHalfBreadth = adjacentOffsets.Values.Max();

                    // Only apply fix if bow is significantly wider than adjacent (scaling artifact)
                    // Bulbous bows may have wider bulb, so be more lenient
                    var threshold = hasBulb ? 1.3m : 1.2m; // 30% for bulbous, 20% for others
                    if (maxBowHalfBreadth > maxAdjacentHalfBreadth * threshold || maxBowHalfBreadth > beamM * 0.45m)
                    {
                        // Cap bow half-breadth progressively from keel to deck
                        var sortedHeights = bowOffsets.Keys.OrderBy(h => h).ToList();
                        var maxHeight = sortedHeights.Count > 0 ? sortedHeights[sortedHeights.Count - 1] : draftM;
                        if (maxHeight <= 0) maxHeight = draftM;

                        // Adaptive: cap based on hull size and bow family
                        int numBowStations = Math.Max(1, Math.Min(4, (int)Math.Ceiling(stations.Count * 0.2m)));
                        if (stations.Count < 10)
                        {
                            numBowStations = 1; // Only fix the very last station for small hulls
                        }

                        var forwardness = 1.0m; // Last station is fully forward

                        // Adjust caps based on bow family
                        // Bulbous bow: More lenient (bulb can be wider)
                        // Wave piercing: More lenient (high flare can be wider)
                        // Standard: Stricter
                        decimal maxStationHalfBreadth;
                        decimal maxKeelHalfBreadth;
                        if (hasBulb)
                        {
                            maxStationHalfBreadth = beamM * (0.20m + 0.25m * (1m - forwardness)); // More lenient for bulb
                            maxKeelHalfBreadth = beamM * (0.03m + 0.04m * (1m - forwardness)); // More lenient
                        }
                        else if (beta > 20m)
                        {
                            maxStationHalfBreadth = beamM * (0.18m + 0.27m * (1m - forwardness)); // More lenient for wave piercing
                            maxKeelHalfBreadth = beamM * (0.025m + 0.035m * (1m - forwardness));
                        }
                        else
                        {
                            maxStationHalfBreadth = beamM * (0.15m + 0.3m * (1m - forwardness)); // Standard
                            maxKeelHalfBreadth = beamM * (0.02m + 0.03m * (1m - forwardness));
                        }

                        // Fix keel (height = 0)
                        if (bowOffsets.ContainsKey(0m) && bowOffsets[0m] > maxKeelHalfBreadth)
                        {
                            bowOffsets[0m] = Math.Min(bowOffsets[0m], maxKeelHalfBreadth);
                        }

                        // Fix other heights
                        for (int i = 1; i < sortedHeights.Count; i++)
                        {
                            var height = sortedHeights[i];
                            var prevHeight = sortedHeights[i - 1];
                            var currentHalfBreadth = bowOffsets[height];
                            var prevHalfBreadth = bowOffsets[prevHeight];
                            var heightNorm = maxHeight > 0 ? height / maxHeight : 0m;

                            // Cap maximum half-breadth
                            if (currentHalfBreadth > maxStationHalfBreadth)
                            {
                                var maxAllowed = maxStationHalfBreadth * (0.3m + 0.7m * heightNorm);
                                bowOffsets[height] = Math.Min(currentHalfBreadth, maxAllowed);
                            }

                            // Ensure smooth tapering (less aggressive for bulbous/wave piercing)
                            var taperThreshold = hasBulb || beta > 20m ? 0.8m : 0.85m; // More lenient for special bows
                            if (currentHalfBreadth < prevHalfBreadth * taperThreshold)
                            {
                                var taperFactor = hasBulb || beta > 20m ? 0.85m : 0.9m; // More lenient
                                bowOffsets[height] = Math.Max(bowOffsets[height], prevHalfBreadth * taperFactor);
                            }
                        }
                    }
                }
            }
        }

        // Ensure stern closure (first station, position closest to 0.0)
        // CRITICAL: Respect user-selected stern family (transom, cruiser, canoe)
        var sternStation = stations
            .Select((s, idx) => new { Station = s, Index = idx, Position = s.Position })
            .OrderBy(x => x.Position)
            .FirstOrDefault();

        if (sternStation != null && sternStation.Position < 0.05m) // Stern station (near position 0.0)
        {
            var sternOffsets = sternStation.Station.Offsets;
            if (sternOffsets != null && sternOffsets.Count > 0)
            {
                // Detect stern family from parameters to adjust closure logic
                var atransNorm = shipdVector[22]; // Transom area coefficient (normalized 0-1)
                var isTransomStern = atransNorm > 0.5m; // Transom when > 0.5, cruiser/canoe when <= 0.5
                var bcTransNorm = shipdVector[28]; // Transom width ratio (normalized 0-1)

                // Find adjacent station (second station)
                var adjacentStation = stations
                    .Select((s, idx) => new { Station = s, Index = idx, Position = s.Position })
                    .Where(x => x.Position > sternStation.Position)
                    .OrderBy(x => x.Position)
                    .FirstOrDefault();

                if (adjacentStation != null && adjacentStation.Station.Offsets != null)
                {
                    var adjacentOffsets = adjacentStation.Station.Offsets;
                    var maxSternHalfBreadth = sternOffsets.Values.Max();
                    var maxAdjacentHalfBreadth = adjacentOffsets.Values.Max();

                    // TRANSOM STERN: Can be wide at deck (up to 70-100% of beam based on bcTransNorm)
                    // Only apply fix if stern is unreasonably wide (more than transom width + margin)
                    if (isTransomStern)
                    {
                        // Transom width is typically 70-100% of beam (bcTransNorm controls this)
                        var transomWidthRatio = 0.7m + bcTransNorm * 0.3m; // 0.7 to 1.0
                        var maxTransomHalfBreadth = (beamM / 2m) * transomWidthRatio;

                        // Only fix if stern is significantly wider than allowed transom width
                        // Allow 10% margin for smooth transition
                        if (maxSternHalfBreadth > maxTransomHalfBreadth * 1.1m || maxSternHalfBreadth > beamM * 0.55m)
                        {
                            var sortedHeights = sternOffsets.Keys.OrderBy(h => h).ToList();
                            var maxHeight = sortedHeights.Count > 0 ? sortedHeights[sortedHeights.Count - 1] : draftM;
                            if (maxHeight <= 0) maxHeight = draftM;

                            // Ensure keel (height = 0) has zero or very small half-breadth
                            if (sternOffsets.ContainsKey(0m))
                            {
                                var keelHalfBreadth = sternOffsets[0m];
                                var maxKeelHalfBreadth = beamM * 0.05m; // Max 5% of beam at keel
                                if (keelHalfBreadth > maxKeelHalfBreadth)
                                {
                                    sternOffsets[0m] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
                                }
                            }

                            // Cap transom stern: wider at deck, narrower at keel
                            for (int i = 1; i < sortedHeights.Count; i++)
                            {
                                var height = sortedHeights[i];
                                var prevHeight = sortedHeights[i - 1];
                                var currentHalfBreadth = sternOffsets[height];
                                var prevHalfBreadth = sternOffsets[prevHeight];
                                var heightNorm = maxHeight > 0 ? height / maxHeight : 0m;

                                // Allow transom to be wider at deck (up to transom width)
                                var maxAllowedForHeight = maxTransomHalfBreadth * (0.3m + 0.7m * heightNorm);
                                if (currentHalfBreadth > maxAllowedForHeight)
                                {
                                    sternOffsets[height] = Math.Min(currentHalfBreadth, maxAllowedForHeight);
                                }

                                // Ensure smooth tapering (less aggressive for transom)
                                if (currentHalfBreadth < prevHalfBreadth * 0.75m)
                                {
                                    sternOffsets[height] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.8m);
                                }
                            }
                        }
                    }
                    else
                    {
                        // CRUISER/CANOE STERN: Should taper smoothly to tip
                        // Only apply fix if stern is significantly wider than adjacent (scaling artifact)
                        if (maxSternHalfBreadth > maxAdjacentHalfBreadth * 1.2m)
                        {
                            // Cap stern half-breadth relative to adjacent station, allowing slight taper
                            var maxAllowedSternHalfBreadth = maxAdjacentHalfBreadth * 1.1m; // Allow 10% wider than adjacent
                            if (maxAllowedSternHalfBreadth > beamM * 0.4m) // Absolute fallback cap
                            {
                                maxAllowedSternHalfBreadth = beamM * 0.4m;
                            }

                            var sortedHeights = sternOffsets.Keys.OrderBy(h => h).ToList();
                            var maxHeight = sortedHeights.Count > 0 ? sortedHeights[sortedHeights.Count - 1] : draftM;
                            if (maxHeight <= 0) maxHeight = draftM;

                            // Ensure keel (height = 0) has zero or very small half-breadth
                            if (sternOffsets.ContainsKey(0m))
                            {
                                var keelHalfBreadth = sternOffsets[0m];
                                var maxKeelHalfBreadth = beamM * 0.05m; // Max 5% of beam at keel
                                if (keelHalfBreadth > maxKeelHalfBreadth)
                                {
                                    sternOffsets[0m] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
                                }
                            }

                            // Ensure the stern tapers properly from keel to deck (cruiser/canoe should taper)
                            for (int i = 1; i < sortedHeights.Count; i++)
                            {
                                var height = sortedHeights[i];
                                var prevHeight = sortedHeights[i - 1];
                                var currentHalfBreadth = sternOffsets[height];
                                var prevHalfBreadth = sternOffsets[prevHeight];
                                var heightNorm = maxHeight > 0 ? height / maxHeight : 0m;

                                // Cap only if unreasonably large (cruiser/canoe should taper, not be wide)
                                if (currentHalfBreadth > maxAllowedSternHalfBreadth)
                                {
                                    // Cruiser/canoe sterns taper more than transom (allow less width at deck)
                                    var maxAllowed = maxAllowedSternHalfBreadth * (0.3m + 0.5m * heightNorm); // Less width at deck
                                    sternOffsets[height] = Math.Min(currentHalfBreadth, maxAllowed);
                                }

                                // Ensure smooth tapering (more aggressive for cruiser/canoe)
                                if (currentHalfBreadth < prevHalfBreadth * 0.8m)
                                {
                                    sternOffsets[height] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.85m);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates bulb offsets for a station in the bow region
    /// Enhanced to match frontend implementation with asymmetry (Lbbm) and fillet radius (Rbb) support
    /// </summary>
    private Dictionary<decimal, decimal> GenerateBulbOffsets(
        decimal stationPos,
        Dictionary<int, decimal> denormalized,
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM)
    {
        var offsets = new Dictionary<decimal, decimal>();

        // Bulb parameters - denormalized values are already the ratios to use
        var lbb = denormalized[33]; // Bulb length ratio (denormalized: actual ratio range 0-0.2)
        var hbb = denormalized[34]; // Bulb height ratio (denormalized: actual ratio range 0-1)
        var bbb = denormalized[35]; // Bulb width ratio (denormalized: actual ratio range 0-1)
        var lbbm = denormalized[36]; // Bulb asymmetry (fore/aft position) - denormalized: actual range -1 to +1
        var rbb = denormalized[37]; // Bulb fillet radius - denormalized: actual range 0.05-0.33

        // Bulb is only in forward section
        // CRITICAL: Use normalized vector for region boundaries (consistent with line 44-45)
        var lb = shipdVector[1]; // Bow length ratio (normalized 0-1)
        var ls = shipdVector[2]; // Stern length ratio (normalized 0-1)
        var bowStart = 1.0m - lb; // Start of bow region (0 = aft, 1 = forward)
        // Bulb extent: lbb is denormalized ratio (0-0.2), which is already the ratio of Lpp
        // Since stationPos is normalized (0-1), bulbExtent should also be normalized
        // lbb is already a ratio (e.g., 0.1 = 10% of Lpp), so normalized extent = lbb
        // Take 70% of bulb length for extent
        var bulbExtent = lbb * 0.7m; // Bulb extends forward (70% of bulb length ratio, normalized)

        if (stationPos > bowStart && stationPos < bowStart + bulbExtent)
        {
            // Position within bulb (0 = start, 1 = forward end)
            var bulbPos = (stationPos - bowStart) / bulbExtent;

            // Apply asymmetry: lbbm shifts max bulb diameter fore/aft
            // lbbm range: -1 to +1, where 0.5 is neutral
            // Convert to -0.15 to +0.15 shift
            var asymmetryShift = (lbbm - 0.5m) * 0.3m; // -0.15 to +0.15
            var adjustedPos = Math.Max(0m, Math.Min(1m, bulbPos + asymmetryShift));

            var bulbHeight = hbb * draftM;
            var bulbWidth = bbb * beamM;

            // Longitudinal profile: use fillet radius to control shape
            // Higher rbb = rounder, lower rbb = more pointed
            // rbb range: 0.05 to 0.33, convert to exponent range 1.5-2.5
            var longitudinalExp = 1.5m + rbb * 1.0m; // 1.5-2.5
            var longitudinalProfile = (decimal)Math.Pow(
                1.0 - Math.Pow((double)adjustedPos, (double)longitudinalExp),
                1.0 / (double)longitudinalExp);

            var heightSteps = 12;
            for (int h = 0; h <= heightSteps; h++)
            {
                var height = (decimal)h / heightSteps * bulbHeight;
                var heightRatio = height / bulbHeight;

                // Vertical profile: ellipsoid with fillet control
                // Higher rbb = rounder (lower exponent), lower rbb = more pointed (higher exponent)
                var verticalExp = 1.8m + (1m - rbb) * 0.5m; // 1.8-2.3 (inverse relationship)
                var verticalProfile = (decimal)Math.Pow(
                    1.0 - Math.Pow((double)heightRatio, (double)verticalExp),
                    1.0 / (double)verticalExp);

                var halfBreadth = (bulbWidth / 2m) * longitudinalProfile * verticalProfile;
                offsets[height] = Math.Max(0m, halfBreadth);
            }
        }

        return offsets;
    }

    /// <summary>
    /// Generates skeg offsets for a station in the stern region
    /// Skeg is a stern appendage that extends below the keel or at a vertical offset
    /// Supports both single skeg and twin skeg configurations
    /// </summary>
    private Dictionary<decimal, decimal> GenerateSkegOffsets(
        decimal stationPos,
        Dictionary<int, decimal> denormalized,
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM)
    {
        var offsets = new Dictionary<decimal, decimal>();

        // Skeg parameters
        var skZ = denormalized[23]; // SK_z - vertical offset control (0-1 normalized)
        var kappaSB = denormalized[38]; // Kappa_SB - skeg curvature parameter
        var lsb = denormalized[39]; // Lsb - skeg length ratio (0-1 normalized, range: 0-0.2)
        var hsb = denormalized[41]; // Hsb - skeg height ratio (for twin skeg, 0-1 normalized)
        var bsb = denormalized[42]; // Bsb - skeg breadth ratio (0-1 normalized)
        var lsbm = denormalized[43]; // Lsbm - skeg longitudinal moment coefficient (-1 to +1 normalized)
        var rsb = denormalized[44]; // Rsb - skeg radius coefficient (0.05-0.33 normalized)

        // Check if twin skeg (bit_SB > 0.5) or single skeg
        // For twin skeg, use Hsb; for single skeg, use HSBOA (index 40)
        var isTwinSkeg = shipdVector[32] > 0.5m; // bit_SB
        var hsboa = denormalized[40]; // HSBOA - skeg height to breadth ratio (for single skeg)

        // Skeg is only in stern region
        // CRITICAL: stationPos is normalized (0-1), where 0=aft, 1=forward
        // ls (normalized) is the normalized position where stern ends (e.g., 0.3 means stern is 30% of normalized length)
        // lsb (denormalized) is the skeg length as a ratio (0-0.2 range, e.g., 0.1 = 10% ratio)
        // Since skeg is within stern region, and lsb is a ratio, we need to determine what lsb is a ratio of
        // Most likely: lsb is ratio of stern length, so skeg extent = lsb * ls (normalized stern length)
        var ls = shipdVector[2]; // Stern length ratio (normalized 0-1, position where stern ends)
        var skegStart = 0.0m; // Start at stern tip (position 0.0 = aft)
        // lsb is denormalized ratio (0-0.2), ls is normalized position (0-1)
        // If lsb is ratio of stern length: skeg extent = lsb * ls (both as ratios in normalized space)
        // This gives skeg extent as a fraction of the normalized coordinate system
        var skegExtent = lsb * ls; // Skeg extends aft from stern tip (normalized extent)

        if (stationPos >= skegStart && stationPos <= skegExtent)
        {
            // Position within skeg (0 = stern tip, 1 = skeg end)
            var skegPos = skegExtent > 0.001m ? stationPos / skegExtent : 0m;

            // Apply longitudinal moment (Lsbm) for asymmetry
            // lsbm range: -1 to +1, where 0.5 is neutral
            // Convert to -0.3 to +0.3 shift
            var asymmetryShift = (lsbm - 0.5m) * 0.6m; // -0.3 to +0.3
            var adjustedPos = Math.Max(0m, Math.Min(1m, skegPos + asymmetryShift));

            // Skeg dimensions
            var skegHeight = isTwinSkeg ? hsb * draftM : hsboa * beamM; // Height for twin skeg, height-to-breadth ratio for single
            var skegBreadth = bsb * beamM;
            var skegVerticalOffset = skZ * draftM; // SK_z controls vertical position (0 = keel, 1 = draft)

            // Skeg extends below keel (negative heights) or at vertical offset
            // Vertical offset: positive = above keel, negative = below keel
            var skegBaseHeight = skegVerticalOffset - skegHeight; // Base of skeg (may be negative)

            // Longitudinal profile: use fillet radius to control shape
            // Higher rsb = rounder, lower rsb = more pointed
            // rsb range: 0.05-0.33, convert to exponent range 1.5-2.5
            var longitudinalExp = 1.5m + rsb * 1.0m; // 1.5-2.5
            var longitudinalProfile = (decimal)Math.Pow(
                1.0 - Math.Pow((double)adjustedPos, (double)longitudinalExp),
                1.0 / (double)longitudinalExp);

            // Generate skeg offsets at various heights
            var heightSteps = 12;
            for (int h = 0; h <= heightSteps; h++)
            {
                var heightRatio = (decimal)h / heightSteps; // 0 to 1
                var height = skegBaseHeight + heightRatio * skegHeight; // Height from keel (may be negative)

                // Vertical profile: ellipsoid with fillet control
                // Higher rsb = rounder (lower exponent), lower rsb = more pointed (higher exponent)
                var verticalExp = 1.8m + (1m - rsb) * 0.5m; // 1.8-2.3 (inverse relationship)
                var verticalProfile = (decimal)Math.Pow(
                    1.0 - Math.Pow((double)heightRatio, (double)verticalExp),
                    1.0 / (double)verticalExp);

                // Apply curvature parameter (Kappa_SB) for convex/concave control
                var curvatureEffect = 1.0m;
                if (Math.Abs(kappaSB - 0.5m) > 0.1m)
                {
                    var convexEffect = (kappaSB - 0.5m) * 2m * (decimal)Math.Sin((double)(heightRatio * (decimal)Math.PI / 2m)) * 0.1m;
                    curvatureEffect = 1m + convexEffect;
                }

                var halfBreadth = (skegBreadth / 2m) * longitudinalProfile * verticalProfile * curvatureEffect;
                offsets[height] = Math.Max(0m, halfBreadth);
            }
        }

        return offsets;
    }

    /// <summary>
    /// Ensures curvature parameters (Rc, Rk, Kappa) are set to sensible defaults to prevent unfair hulls.
    /// This fixes the issue where zero or extreme curvature values cause zig-zag patterns in waterlines
    /// and jagged buttocks, resulting in C² discontinuity.
    /// </summary>
    /// <param name="shipdVector">ShipD parameter vector (will be modified in place)</param>
    /// <param name="metadata">Parameter metadata</param>
    private void EnsureSensibleCurvatureParameters(
        decimal[] shipdVector,
        IReadOnlyList<ShipDParameterMetadataDto> metadata)
    {
        bool updated = false;

        // Rc (index 9) - Bow curvature coefficient: should be 0.2-0.4 for smooth bilge radius
        // Problem: Zero values (0.000) create hard chines and unfair geometry
        var rcParam = metadata.FirstOrDefault(m => m.ParameterIndex == 9);
        if (rcParam != null)
        {
            var rcNorm = shipdVector[9];
            // Check if value is too small (< 0.05) or negative, indicating hard chine
            if (rcNorm < 0.05m || rcNorm <= 0m)
            {
                var oldValue = rcNorm;
                // Default to 0.3 (middle of 0.2-0.4 range) for smooth curvature
                shipdVector[9] = Math.Clamp(0.3m, rcParam.Min ?? 0m, rcParam.Max ?? 1m);
                updated = true;
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Enforced Rc (bow curvature): {OldValue} -> {NewValue} (recommended: 0.2-0.4 for smooth bilge)",
                    oldValue, shipdVector[9]);
            }
        }

        // Rk (index 10) - Bow knuckle coefficient: should be 0.1-0.3 for smooth deck edge transition
        // Problem: Zero or extreme negative values (-0.999, -1.000) create sharp transitions
        var rkParam = metadata.FirstOrDefault(m => m.ParameterIndex == 10);
        if (rkParam != null)
        {
            var rkNorm = shipdVector[10];
            // Check if value is too small (< 0.05), negative, or extreme negative (< -0.5)
            if (rkNorm < 0.05m || rkNorm <= 0m || rkNorm < -0.5m)
            {
                var oldValue = rkNorm;
                // Default to 0.2 (middle of 0.1-0.3 range) for smooth knuckle
                shipdVector[10] = Math.Clamp(0.2m, rkParam.Min ?? -1m, rkParam.Max ?? 1m);
                updated = true;
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Enforced Rk (bow knuckle): {OldValue} -> {NewValue} (recommended: 0.1-0.3 for smooth transition)",
                    oldValue, shipdVector[10]);
            }
        }

        // Kappa_bow (index 14) - Bow sectional curvature: should be 0.4-0.6 for neutral/smooth sections
        // Problem: Zero values (0.000) disable curvature control, leading to unfair geometry
        var kappaBowParam = metadata.FirstOrDefault(m => m.ParameterIndex == 14);
        if (kappaBowParam != null)
        {
            var kappaBowNorm = shipdVector[14];
            // Check if value is too close to zero (< 0.1) or exactly zero
            // Kappa should be around 0.5 (neutral) for smooth sections
            if (Math.Abs(kappaBowNorm) < 0.1m || kappaBowNorm == 0m)
            {
                var oldValue = kappaBowNorm;
                // Default to 0.5 (neutral) for smooth sectional curvature
                shipdVector[14] = Math.Clamp(0.5m, kappaBowParam.Min ?? 0m, kappaBowParam.Max ?? 1m);
                updated = true;
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Enforced Kappa_bow (bow sectional curvature): {OldValue} -> {NewValue} (recommended: 0.4-0.6 for smooth sections)",
                    oldValue, shipdVector[14]);
            }
        }

        // Rc_trans (index 29) - Stern curvature coefficient: should be 0.2-0.4 for smooth stern transition
        var rcTransParam = metadata.FirstOrDefault(m => m.ParameterIndex == 29);
        if (rcTransParam != null)
        {
            var rcTransNorm = shipdVector[29];
            if (rcTransNorm < 0.05m || rcTransNorm <= 0m)
            {
                var oldValue = rcTransNorm;
                shipdVector[29] = Math.Clamp(0.3m, rcTransParam.Min ?? 0m, rcTransParam.Max ?? 1m);
                updated = true;
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Enforced Rc_trans (stern curvature): {OldValue} -> {NewValue} (recommended: 0.2-0.4 for smooth transition)",
                    oldValue, shipdVector[29]);
            }
        }

        // Rk_trans (index 30) - Stern knuckle coefficient: should be 0.0-0.2 for smooth stern sheer
        var rkTransParam = metadata.FirstOrDefault(m => m.ParameterIndex == 30);
        if (rkTransParam != null)
        {
            var rkTransNorm = shipdVector[30];
            // Check if value is too small (< 0.02), negative, or extreme negative
            if (rkTransNorm < 0.02m || rkTransNorm <= 0m || rkTransNorm < -0.5m)
            {
                var oldValue = rkTransNorm;
                // Default to 0.1 (middle of 0.0-0.2 range) for smooth knuckle
                shipdVector[30] = Math.Clamp(0.1m, rkTransParam.Min ?? -1m, rkTransParam.Max ?? 1m);
                updated = true;
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Enforced Rk_trans (stern knuckle): {OldValue} -> {NewValue} (recommended: 0.0-0.2 for smooth transition)",
                    oldValue, shipdVector[30]);
            }
        }

        // Kappa_stern (index 24) - Stern sectional curvature: should be 0.4-0.6 for neutral/smooth sections
        var kappaSternParam = metadata.FirstOrDefault(m => m.ParameterIndex == 24);
        if (kappaSternParam != null)
        {
            var kappaSternNorm = shipdVector[24];
            if (Math.Abs(kappaSternNorm) < 0.1m || kappaSternNorm == 0m)
            {
                var oldValue = kappaSternNorm;
                // Default to 0.5 (neutral) for smooth sectional curvature
                shipdVector[24] = Math.Clamp(0.5m, kappaSternParam.Min ?? 0m, kappaSternParam.Max ?? 1m);
                updated = true;
                _logger.LogWarning(
                    "[SHIPD_GEOMETRY] Enforced Kappa_stern (stern sectional curvature): {OldValue} -> {NewValue} (recommended: 0.4-0.6 for smooth sections)",
                    oldValue, shipdVector[24]);
            }
        }

        if (updated)
        {
            _logger.LogWarning(
                "[SHIPD_GEOMETRY] ✅ Applied sensible curvature parameter defaults to prevent unfair hull geometry. " +
                "Rc={Rc}, Rk={Rk}, Kappa_bow={KappaBow}, Rc_trans={RcTrans}, Rk_trans={RkTrans}, Kappa_stern={KappaStern}",
                shipdVector[9], shipdVector[10], shipdVector[14], shipdVector[29], shipdVector[30], shipdVector[24]);
        }
    }
}
