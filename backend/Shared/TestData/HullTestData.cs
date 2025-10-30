namespace Shared.TestData;

/// <summary>
/// Test data generators for standard hull forms
/// Used for validation and testing of hydrostatic calculations
/// </summary>
public static class HullTestData
{
    /// <summary>
    /// Generates a rectangular barge for analytical validation
    /// All hydrostatic properties can be calculated analytically
    /// </summary>
    /// <param name="length">Length (m)</param>
    /// <param name="beam">Beam (m)</param>
    /// <param name="designDraft">Design draft (m)</param>
    /// <param name="numStations">Number of stations</param>
    /// <param name="numWaterlines">Number of waterlines</param>
    public static (List<StationData> stations, List<WaterlineData> waterlines, List<OffsetData> offsets)
        GenerateRectangularBarge(
            decimal length = 100m,
            decimal beam = 20m,
            decimal designDraft = 10m,
            int numStations = 5,
            int numWaterlines = 3)
    {
        var stations = new List<StationData>();
        var waterlines = new List<WaterlineData>();
        var offsets = new List<OffsetData>();

        // Generate equally spaced stations along length
        for (int i = 0; i < numStations; i++)
        {
            decimal x = length * i / (numStations - 1);
            stations.Add(new StationData { Index = i, X = x });
        }

        // Generate equally spaced waterlines up to design draft
        for (int j = 0; j < numWaterlines; j++)
        {
            decimal z = designDraft * j / (numWaterlines - 1);
            waterlines.Add(new WaterlineData { Index = j, Z = z });
        }

        // Generate offsets - constant half-breadth (rectangular box)
        decimal halfBreadth = beam / 2m;
        for (int i = 0; i < numStations; i++)
        {
            for (int j = 0; j < numWaterlines; j++)
            {
                offsets.Add(new OffsetData
                {
                    StationIndex = i,
                    WaterlineIndex = j,
                    HalfBreadthY = halfBreadth
                });
            }
        }

        return (stations, waterlines, offsets);
    }

    /// <summary>
    /// Generates a Wigley hull form for benchmark testing
    /// Wigley parabolic hull: y = (B/2) * (1 - z²) * (1 - x²)
    /// Reference: J.H. Michell (1898), Wigley (1942)
    /// </summary>
    /// <param name="length">Length (m)</param>
    /// <param name="beam">Beam (m)</param>
    /// <param name="designDraft">Design draft (m)</param>
    /// <param name="numStations">Number of stations (should be odd for Simpson's rule)</param>
    /// <param name="numWaterlines">Number of waterlines (should be odd for Simpson's rule)</param>
    public static (List<StationData> stations, List<WaterlineData> waterlines, List<OffsetData> offsets)
        GenerateWigleyHull(
            decimal length = 100m,
            decimal beam = 10m,
            decimal designDraft = 6.25m,
            int numStations = 21,
            int numWaterlines = 13)
    {
        var stations = new List<StationData>();
        var waterlines = new List<WaterlineData>();
        var offsets = new List<OffsetData>();

        // Wigley hull is typically defined from -L/2 to +L/2
        // We'll shift to 0 to L for consistency
        decimal halfLength = length / 2m;

        // Generate stations
        for (int i = 0; i < numStations; i++)
        {
            // Map from 0 to length, then normalize to -1 to +1 for Wigley formula
            decimal x = length * i / (numStations - 1);
            stations.Add(new StationData { Index = i, X = x });
        }

        // Generate waterlines
        // Note: Wigley formula uses z from 0 to 1, not -1 to +1
        for (int j = 0; j < numWaterlines; j++)
        {
            decimal z = designDraft * j / (numWaterlines - 1);
            waterlines.Add(new WaterlineData { Index = j, Z = z });
        }

        // Generate offsets using Wigley formula
        // y = (B/2) * (1 - z²) * (1 - x²)
        // where x and z are normalized to [-1, 1]
        for (int i = 0; i < numStations; i++)
        {
            decimal x = stations[i].X;
            // Normalize x to [-1, 1]
            decimal xNorm = (x - halfLength) / halfLength;

            for (int j = 0; j < numWaterlines; j++)
            {
                decimal z = waterlines[j].Z;
                // Normalize z to [0, 1] for Wigley formula
                // z goes from 0 (keel) to 1 (waterline)
                decimal zNorm = z / designDraft;

                // Wigley formula: y = (B/2) * (1 - z²) * (1 - x²)
                decimal xTerm = 1m - xNorm * xNorm;
                decimal zTerm = 1m - zNorm * zNorm;
                decimal halfBreadth = (beam / 2m) * xTerm * zTerm;

                // Ensure non-negative
                halfBreadth = Math.Max(0m, halfBreadth);

                offsets.Add(new OffsetData
                {
                    StationIndex = i,
                    WaterlineIndex = j,
                    HalfBreadthY = halfBreadth
                });
            }
        }

        return (stations, waterlines, offsets);
    }

    /// <summary>
    /// Analytical hydrostatic properties for rectangular barge
    /// Used for validation (<0.5% error tolerance)
    /// </summary>
    public static AnalyticalHydrostatics GetRectangularBargeAnalytical(
        decimal length,
        decimal beam,
        decimal draft,
        decimal rho = 1025m)
    {
        // Volume: ∇ = L * B * T
        decimal volume = length * beam * draft;

        // Displacement: ∆ = ρ * ∇
        decimal displacement = rho * volume;

        // Center of Buoyancy (vertical): KB = T / 2
        decimal kb = draft / 2m;

        // Longitudinal Center of Buoyancy: LCB = L / 2 (at midship)
        decimal lcb = length / 2m;

        // Transverse Center of Buoyancy: TCB = 0 (symmetric)
        decimal tcb = 0m;

        // Waterplane area: Awp = L * B
        decimal awp = length * beam;

        // Transverse second moment of waterplane area: I_t = (L * B³) / 12
        decimal iWpT = (length * beam * beam * beam) / 12m;

        // Longitudinal second moment of waterplane area: I_l = (B * L³) / 12
        decimal iWpL = (beam * length * length * length) / 12m;

        // Transverse metacentric radius: BM_t = I_t / ∇
        decimal bmt = iWpT / volume;

        // Longitudinal metacentric radius: BM_l = I_l / ∇
        decimal bml = iWpL / volume;

        // Form coefficients (all 1.0 for rectangular box)
        decimal cb = 1.0m;  // Block coefficient
        decimal cp = 1.0m;  // Prismatic coefficient
        decimal cm = 1.0m;  // Midship coefficient
        decimal cwp = 1.0m; // Waterplane coefficient

        return new AnalyticalHydrostatics
        {
            Volume = volume,
            Displacement = displacement,
            KB = kb,
            LCB = lcb,
            TCB = tcb,
            Awp = awp,
            IwpTransverse = iWpT,
            IwpLongitudinal = iWpL,
            BMt = bmt,
            BMl = bml,
            Cb = cb,
            Cp = cp,
            Cm = cm,
            Cwp = cwp
        };
    }

    /// <summary>
    /// Approximate analytical properties for Wigley hull
    /// Note: Exact values require numerical integration
    /// These are reference values for validation (~2% tolerance)
    /// </summary>
    public static AnalyticalHydrostatics GetWigleyHullApproximate(
        decimal length,
        decimal beam,
        decimal draft,
        decimal rho = 1025m)
    {
        // For Wigley hull at design draft (T = B/1.6), typical coefficients:
        // Cb ≈ 0.444 (from literature)
        // Cp ≈ 0.667
        // Cm ≈ 0.667
        // Cwp ≈ 0.667

        // Volume: ∇ = L * B * T * Cb
        decimal cb = 0.444m;
        decimal volume = length * beam * draft * cb;

        // Displacement
        decimal displacement = rho * volume;

        // KB ≈ T / 2.4 (approximate for parabolic sections)
        decimal kb = draft / 2.4m;

        // LCB at midship (symmetric hull)
        decimal lcb = length / 2m;

        // Waterplane area: Awp = L * B * Cwp
        decimal cwp = 0.667m;
        decimal awp = length * beam * cwp;

        // BM_t ≈ (B²) / (10.8 * T) (approximate for Wigley)
        decimal bmt = (beam * beam) / (10.8m * draft);

        // BM_l ≈ (L²) / (24 * T) (approximate)
        decimal bml = (length * length) / (24m * draft);

        return new AnalyticalHydrostatics
        {
            Volume = volume,
            Displacement = displacement,
            KB = kb,
            LCB = lcb,
            TCB = 0m,
            Awp = awp,
            IwpTransverse = bmt * volume,
            IwpLongitudinal = bml * volume,
            BMt = bmt,
            BMl = bml,
            Cb = cb,
            Cp = 0.667m,
            Cm = 0.667m,
            Cwp = cwp
        };
    }
}

/// <summary>
/// Station data for test generation
/// </summary>
public record StationData
{
    public int Index { get; init; }
    public decimal X { get; init; }
}

/// <summary>
/// Waterline data for test generation
/// </summary>
public record WaterlineData
{
    public int Index { get; init; }
    public decimal Z { get; init; }
}

/// <summary>
/// Offset data for test generation
/// </summary>
public record OffsetData
{
    public int StationIndex { get; init; }
    public int WaterlineIndex { get; init; }
    public decimal HalfBreadthY { get; init; }
}

/// <summary>
/// Analytical hydrostatic properties for validation
/// </summary>
public record AnalyticalHydrostatics
{
    public decimal Volume { get; init; }
    public decimal Displacement { get; init; }
    public decimal KB { get; init; }
    public decimal LCB { get; init; }
    public decimal TCB { get; init; }
    public decimal Awp { get; init; }
    public decimal IwpTransverse { get; init; }
    public decimal IwpLongitudinal { get; init; }
    public decimal BMt { get; init; }
    public decimal BMl { get; init; }
    public decimal Cb { get; init; }
    public decimal Cp { get; init; }
    public decimal Cm { get; init; }
    public decimal Cwp { get; init; }
}
