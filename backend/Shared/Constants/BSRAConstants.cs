namespace Shared.Constants;

/// <summary>
/// BSRA (British Ship Research Association) reference data constants
/// Source: BSRA Series documentation and reference tables
/// </summary>
public static class BSRAConstants
{
    /// <summary>
    /// BSRA 23-station Simpson's multipliers for numerical integration
    /// Stations: 0 (AP), 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0 (Midship),
    ///           6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.25, 9.5, 9.75, 10.0 (FP)
    /// Common interval: h = Lbp / 10
    /// Volume: V = (h/3) * Σ(areas[i] * multipliers[i])
    /// </summary>
    public static readonly BSRAStationMultiplier[] SimpsonMultipliers = new[]
    {
        new BSRAStationMultiplier { Station = 0.0m, Multiplier = 0.25m, Description = "AP" },
        new BSRAStationMultiplier { Station = 0.25m, Multiplier = 1.0m, Description = "Quarter_station" },
        new BSRAStationMultiplier { Station = 0.5m, Multiplier = 0.5m, Description = "Half_station" },
        new BSRAStationMultiplier { Station = 0.75m, Multiplier = 1.0m, Description = "Three-quarter_station" },
        new BSRAStationMultiplier { Station = 1.0m, Multiplier = 0.75m, Description = "Station_1" },
        new BSRAStationMultiplier { Station = 1.5m, Multiplier = 2.0m, Description = "Station_1.5" },
        new BSRAStationMultiplier { Station = 2.0m, Multiplier = 1.0m, Description = "Station_2" },
        new BSRAStationMultiplier { Station = 2.5m, Multiplier = 2.0m, Description = "Station_2.5" },
        new BSRAStationMultiplier { Station = 3.0m, Multiplier = 1.0m, Description = "Station_3" },
        new BSRAStationMultiplier { Station = 3.5m, Multiplier = 2.0m, Description = "Station_3.5" },
        new BSRAStationMultiplier { Station = 4.0m, Multiplier = 1.5m, Description = "Station_4" },
        new BSRAStationMultiplier { Station = 5.0m, Multiplier = 4.0m, Description = "Midship" },
        new BSRAStationMultiplier { Station = 6.0m, Multiplier = 1.5m, Description = "Station_6" },
        new BSRAStationMultiplier { Station = 6.5m, Multiplier = 2.0m, Description = "Station_6.5" },
        new BSRAStationMultiplier { Station = 7.0m, Multiplier = 1.0m, Description = "Station_7" },
        new BSRAStationMultiplier { Station = 7.5m, Multiplier = 2.0m, Description = "Station_7.5" },
        new BSRAStationMultiplier { Station = 8.0m, Multiplier = 1.0m, Description = "Station_8" },
        new BSRAStationMultiplier { Station = 8.5m, Multiplier = 2.0m, Description = "Station_8.5" },
        new BSRAStationMultiplier { Station = 9.0m, Multiplier = 0.75m, Description = "Station_9" },
        new BSRAStationMultiplier { Station = 9.25m, Multiplier = 1.0m, Description = "Station_9.25" },
        new BSRAStationMultiplier { Station = 9.5m, Multiplier = 0.5m, Description = "Station_9.5" },
        new BSRAStationMultiplier { Station = 9.75m, Multiplier = 1.0m, Description = "Station_9.75" },
        new BSRAStationMultiplier { Station = 10.0m, Multiplier = 0.25m, Description = "FP" }
    };

    /// <summary>
    /// End slopes for cubic spline fairing (Cb=0.80 reference)
    /// Bow slopes: negative (decreasing toward bow/station 0)
    /// Stern slopes: positive (increasing toward stern/station 10)
    /// Used for clamped cubic spline boundary conditions to prevent oscillation
    /// </summary>
    public static readonly WaterlineEndSlope[] EndSlopesCb080 = new[]
    {
        new WaterlineEndSlope { Waterline = 1.0m, BowSlope = -0.5m, SternSlope = 0.8m, Notes = "Standard_slopes" },
        new WaterlineEndSlope { Waterline = 2.0m, BowSlope = -0.6m, SternSlope = 0.9m },
        new WaterlineEndSlope { Waterline = 3.0m, BowSlope = -0.7m, SternSlope = 1.0m },
        new WaterlineEndSlope { Waterline = 4.0m, BowSlope = -0.8m, SternSlope = 1.1m },
        new WaterlineEndSlope { Waterline = 5.0m, BowSlope = -0.9m, SternSlope = 1.2m },
        new WaterlineEndSlope { Waterline = 6.0m, BowSlope = -1.0m, SternSlope = 1.3m },
        new WaterlineEndSlope { Waterline = 7.0m, BowSlope = -1.1m, SternSlope = 1.4m },
        new WaterlineEndSlope { Waterline = 8.0m, BowSlope = -1.2m, SternSlope = 1.5m },
        new WaterlineEndSlope { Waterline = 9.0m, BowSlope = -1.3m, SternSlope = 1.6m },
        new WaterlineEndSlope { Waterline = 10.0m, BowSlope = -1.4m, SternSlope = 1.7m },
        new WaterlineEndSlope { Waterline = 11.0m, BowSlope = -1.5m, SternSlope = 1.8m },
        new WaterlineEndSlope { Waterline = 12.0m, BowSlope = -1.6m, SternSlope = 1.9m },
        new WaterlineEndSlope { Waterline = 13.0m, BowSlope = -1.7m, SternSlope = 2.0m, Notes = "Design_waterline" },
        new WaterlineEndSlope { Waterline = 14.0m, BowSlope = -1.8m, SternSlope = 2.1m },
        new WaterlineEndSlope { Waterline = 15.0m, BowSlope = -1.9m, SternSlope = 2.2m },
        new WaterlineEndSlope { Waterline = 16.0m, BowSlope = -2.0m, SternSlope = 2.3m },
        new WaterlineEndSlope { Waterline = 16.4m, BowSlope = -2.05m, SternSlope = 2.35m, Notes = "Freeboard_deck" }
    };

    /// <summary>
    /// Validation tolerances for offset generation
    /// </summary>
    public static class ValidationTolerances
    {
        public const decimal CbTolerancePercent = 2.0m;
        public const decimal DisplacementTolerancePercent = 2.0m;
        public const decimal VolumeTolerancePercent = 2.0m;
        public const decimal LcbTolerancePercent = 5.0m;
        public const decimal WaterplaneAreaTolerancePercent = 3.0m;
        public const decimal MaxCurvatureChange = 0.5m; // meters
    }

    /// <summary>
    /// Vessel type parameter ranges for validation and default values
    /// </summary>
    public static class VesselTypeRanges
    {
        public static class ProductCarrier
        {
            public const decimal L_B_Min = 5.0m;
            public const decimal L_B_Typical = 6.6m;
            public const decimal L_B_Max = 7.5m;
            public const decimal B_D_Min = 1.5m;
            public const decimal B_D_Typical = 1.7m;
            public const decimal B_D_Max = 2.0m;
            public const decimal L_D_Min = 9.0m;
            public const decimal L_D_Typical = 11.0m;
            public const decimal L_D_Max = 13.0m;
            public const decimal Cb_Min = 0.75m;
            public const decimal Cb_Typical = 0.80m;
            public const decimal Cb_Max = 0.85m;
        }

        public static class BulkCarrier
        {
            public const decimal L_B_Min = 5.5m;
            public const decimal L_B_Typical = 6.8m;
            public const decimal L_B_Max = 8.0m;
            public const decimal B_D_Min = 1.8m;
            public const decimal B_D_Typical = 2.2m;
            public const decimal B_D_Max = 2.5m;
            public const decimal Cb_Min = 0.80m;
            public const decimal Cb_Typical = 0.85m;
            public const decimal Cb_Max = 0.88m;
        }

        public static class Tanker
        {
            public const decimal L_B_Min = 4.5m;
            public const decimal L_B_Typical = 5.5m;
            public const decimal L_B_Max = 6.5m;
            public const decimal B_D_Min = 1.5m;
            public const decimal B_D_Typical = 1.8m;
            public const decimal B_D_Max = 2.2m;
            public const decimal Cb_Min = 0.78m;
            public const decimal Cb_Typical = 0.82m;
            public const decimal Cb_Max = 0.86m;
        }

        public static class Container
        {
            public const decimal L_B_Min = 6.0m;
            public const decimal L_B_Typical = 7.5m;
            public const decimal L_B_Max = 9.0m;
            public const decimal B_D_Min = 2.0m;
            public const decimal B_D_Typical = 2.5m;
            public const decimal B_D_Max = 3.0m;
            public const decimal Cb_Min = 0.55m;
            public const decimal Cb_Typical = 0.60m;
            public const decimal Cb_Max = 0.65m;
        }

        public static class GeneralCargo
        {
            public const decimal L_B_Min = 5.5m;
            public const decimal L_B_Typical = 6.5m;
            public const decimal L_B_Max = 7.5m;
            public const decimal B_D_Min = 1.6m;
            public const decimal B_D_Typical = 1.9m;
            public const decimal B_D_Max = 2.3m;
            public const decimal Cb_Min = 0.68m;
            public const decimal Cb_Typical = 0.72m;
            public const decimal Cb_Max = 0.76m;
        }
    }
}

/// <summary>
/// BSRA station multiplier for Simpson's rule integration
/// </summary>
public class BSRAStationMultiplier
{
    public decimal Station { get; init; }
    public decimal Multiplier { get; init; }
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// End slopes for cubic spline fairing at a specific waterline
/// </summary>
public class WaterlineEndSlope
{
    public decimal Waterline { get; init; }
    public decimal BowSlope { get; init; }
    public decimal SternSlope { get; init; }
    public string? Notes { get; init; }
}
