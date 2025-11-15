namespace DataService.Tests.TestData;

/// <summary>
/// Barge GZ curve constants for validation tests
/// Converted from Barge_GZ_Curve.csv for better maintainability and testability
/// </summary>
public static class BargeGZCurveConstants
{
    /// <summary>
    /// Barge GZ curve record
    /// </summary>
    public record BargeGZRecord
    {
        public decimal Angle_deg { get; init; }
        public decimal GZ_m { get; init; }
        public decimal GM_T_m { get; init; }
    }

    /// <summary>
    /// Get all barge GZ curve records
    /// </summary>
    public static List<BargeGZRecord> GetGZCurve()
    {
        return new List<BargeGZRecord>
        {
            new BargeGZRecord { Angle_deg = 0m, GZ_m = 0.0m, GM_T_m = 0.25m },
            new BargeGZRecord { Angle_deg = 5m, GZ_m = 0.02178893568691454m, GM_T_m = 0.25m },
            new BargeGZRecord { Angle_deg = 10m, GZ_m = 0.04341204441673258m, GM_T_m = 0.25m },
            new BargeGZRecord { Angle_deg = 15m, GZ_m = 0.06470476127563018m, GM_T_m = 0.25m },
            new BargeGZRecord { Angle_deg = 20m, GZ_m = 0.08550503583141718m, GM_T_m = 0.25m },
            new BargeGZRecord { Angle_deg = 25m, GZ_m = 0.10565456543517486m, GM_T_m = 0.25m },
            new BargeGZRecord { Angle_deg = 30m, GZ_m = 0.12499999999999999m, GM_T_m = 0.25m }
        };
    }
}

