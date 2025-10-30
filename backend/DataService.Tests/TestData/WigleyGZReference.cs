namespace DataService.Tests.TestData;

/// <summary>
/// Reference data for Wigley hull stability validation
/// Based on published literature and empirical data
/// </summary>
public static class WigleyGZReference
{
    /// <summary>
    /// Expected characteristics for Wigley hull stability
    /// Based on typical naval architecture literature for Wigley parabolic hulls
    /// </summary>
    public static class ExpectedCharacteristics
    {
        /// <summary>
        /// Expected range for angle of maximum GZ (degrees)
        /// Wigley hulls typically show max GZ between 30-50°
        /// </summary>
        public static (decimal min, decimal max) MaxGZAngleRange => (30m, 50m);

        /// <summary>
        /// Expected approximate GMT for Wigley hull with typical proportions
        /// L=100m, B=10m, T=6.25m, KG at 50% of draft
        /// Approximate from form characteristics
        /// </summary>
        public static decimal ApproximateGMT_Typical => 0.8m; // meters, rough approximation

        /// <summary>
        /// Expected approximate area under GZ curve 0-30° for typical Wigley
        /// This is based on similar hull forms, actual value should be computed
        /// </summary>
        public static decimal ApproximateArea_0_30_mrad => 0.08m; // m·rad, approximate

        /// <summary>
        /// Tolerance for GZ curve area comparison (5%)
        /// Due to geometric approximations and integration methods
        /// </summary>
        public static decimal AreaTolerance => 0.05m; // 5%

        /// <summary>
        /// Expected GZ curve shape characteristics
        /// </summary>
        public static string ExpectedShape => "Monotonic increase to peak, then gradual decrease";
    }

    /// <summary>
    /// Validates that GZ curve has expected shape for Wigley hull
    /// </summary>
    /// <param name="gzPoints">GZ curve points (angle, GZ pairs)</param>
    /// <returns>True if shape is reasonable</returns>
    public static bool ValidateGZCurveShape(List<(decimal angle, decimal gz)> gzPoints)
    {
        if (gzPoints.Count < 3)
        {
            return false;
        }

        // Find max GZ
        var maxPoint = gzPoints.MaxBy(p => p.gz);
        if (maxPoint.gz <= 0)
        {
            return false;
        }

        // Check that GZ increases to max, then decreases
        var indexOfMax = gzPoints.IndexOf(maxPoint);

        // Before max: should be generally increasing
        bool increasingBeforeMax = true;
        for (int i = 1; i < indexOfMax; i++)
        {
            // Allow small decreases (numerical noise), but general trend should be up
            if (gzPoints[i].gz < gzPoints[i - 1].gz * 0.95m)
            {
                increasingBeforeMax = false;
                break;
            }
        }

        // After max: should be generally decreasing
        bool decreasingAfterMax = true;
        for (int i = indexOfMax + 1; i < gzPoints.Count; i++)
        {
            // Allow small increases (numerical noise), but general trend should be down
            if (gzPoints[i].gz > gzPoints[i - 1].gz * 1.05m)
            {
                decreasingAfterMax = false;
                break;
            }
        }

        return increasingBeforeMax && decreasingAfterMax;
    }

    /// <summary>
    /// Validates that max GZ occurs in expected range for Wigley hull
    /// </summary>
    public static bool ValidateMaxGZAngle(decimal angleAtMaxGZ)
    {
        var (min, max) = ExpectedCharacteristics.MaxGZAngleRange;
        return angleAtMaxGZ >= min && angleAtMaxGZ <= max;
    }

    /// <summary>
    /// Gets baseline GZ values for comparison
    /// These are approximate baseline values derived from similar hull forms
    /// Used to verify that computed values are in reasonable range
    /// </summary>
    public static List<(decimal angle, decimal approxGZ)> GetBaselineGZValues()
    {
        // Approximate GZ values for typical Wigley hull
        // These are rough estimates based on similar hull forms
        // Actual values will depend on exact geometry
        return new List<(decimal angle, decimal approxGZ)>
        {
            (0m, 0.0m),
            (5m, 0.07m),
            (10m, 0.14m),
            (15m, 0.20m),
            (20m, 0.26m),
            (25m, 0.31m),
            (30m, 0.35m),
            (35m, 0.38m),
            (40m, 0.40m),
            (45m, 0.40m),
            (50m, 0.38m),
            (60m, 0.32m),
            (70m, 0.24m),
            (80m, 0.15m),
            (90m, 0.05m)
        };
    }

    /// <summary>
    /// Checks if computed GZ is within reasonable range of baseline
    /// Uses relaxed tolerance due to geometric variations
    /// </summary>
    public static bool IsGZInReasonableRange(decimal angle, decimal computedGZ, decimal toleranceFactor = 0.3m)
    {
        var baseline = GetBaselineGZValues();
        var closestPoint = baseline.MinBy(p => Math.Abs(p.angle - angle));

        if (closestPoint.approxGZ == 0)
        {
            // Near zero, just check it's small
            return Math.Abs(computedGZ) < 0.1m;
        }

        // Check within tolerance factor
        var ratio = Math.Abs(computedGZ - closestPoint.approxGZ) / closestPoint.approxGZ;
        return ratio <= toleranceFactor;
    }
}







