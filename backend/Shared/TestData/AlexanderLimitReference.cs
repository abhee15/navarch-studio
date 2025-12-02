namespace Shared.TestData;

/// <summary>
/// Alexander Limit reference data for validating Block Coefficient (Cb) against Froude Number (Fn).
///
/// The Alexander Limit establishes the maximum efficient Block Coefficient for a given Froude Number.
/// This fundamental naval architecture principle states that faster ships must be thinner (lower Cb).
///
/// Source: Ship Design Validation Handbook - Validation Charts
///
/// Formula: As Froude Number increases, Block Coefficient must decrease for efficient operation.
/// Designs that violate this limit (Cb too high for speed) are inefficient or physically impossible.
/// </summary>
public static class AlexanderLimitReference
{
    /// <summary>
    /// Data point for Alexander Limit curve
    /// </summary>
    public record AlexanderLimitPoint
    {
        /// <summary>Froude Number</summary>
        public decimal FroudeNumber { get; init; }

        /// <summary>Maximum efficient Block Coefficient for this Froude Number</summary>
        public decimal MaxEfficientCb { get; init; }

        /// <summary>Corresponding speed in knots (for reference, approximate)</summary>
        public decimal? SpeedKn { get; init; }

        /// <summary>Vessel type example</summary>
        public string? VesselTypeExample { get; init; }
    }

    /// <summary>
    /// Reference data points for Alexander Limit curve
    ///
    /// Source: Ship Design Validation Handbook - Chart 1: The Alexander Limit
    /// </summary>
    public static readonly AlexanderLimitPoint[] ReferencePoints = new[]
    {
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.15m,
            MaxEfficientCb = 0.82m,
            SpeedKn = 14.5m,
            VesselTypeExample = "VLCC"
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.18m,
            MaxEfficientCb = 0.80m,
            SpeedKn = null,
            VesselTypeExample = null
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.20m,
            MaxEfficientCb = 0.72m,
            SpeedKn = 19.3m,
            VesselTypeExample = "Cargo/Tanker"
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.21m,
            MaxEfficientCb = 0.71m, // Fixed: Must decrease from 0.20 (was incorrectly 0.73)
            SpeedKn = null,
            VesselTypeExample = null
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.24m,
            MaxEfficientCb = 0.66m,
            SpeedKn = null,
            VesselTypeExample = null
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.25m,
            MaxEfficientCb = 0.65m,
            SpeedKn = 24.1m,
            VesselTypeExample = "Fast Cargo"
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.27m,
            MaxEfficientCb = 0.60m,
            SpeedKn = null,
            VesselTypeExample = null
        },
        new AlexanderLimitPoint
        {
            FroudeNumber = 0.30m,
            MaxEfficientCb = 0.58m,
            SpeedKn = 29.0m,
            VesselTypeExample = "Container"
        }
    };

    /// <summary>
    /// Interpolates the maximum efficient Cb for a given Froude Number using linear interpolation.
    ///
    /// If Fn is below the minimum reference point (0.15), returns the max Cb at 0.15 (0.82).
    /// If Fn is above the maximum reference point (0.30), returns the min Cb at 0.30 (0.58).
    ///
    /// </summary>
    /// <param name="froudeNumber">Froude Number to interpolate for</param>
    /// <returns>Maximum efficient Block Coefficient for the given Froude Number</returns>
    public static decimal InterpolateMaxEfficientCb(decimal froudeNumber)
    {
        if (froudeNumber <= ReferencePoints[0].FroudeNumber)
        {
            // Below minimum - return max Cb from first point
            return ReferencePoints[0].MaxEfficientCb;
        }

        if (froudeNumber >= ReferencePoints[^1].FroudeNumber)
        {
            // Above maximum - return min Cb from last point
            return ReferencePoints[^1].MaxEfficientCb;
        }

        // Find the two points to interpolate between
        for (int i = 0; i < ReferencePoints.Length - 1; i++)
        {
            var lower = ReferencePoints[i];
            var upper = ReferencePoints[i + 1];

            if (froudeNumber >= lower.FroudeNumber && froudeNumber <= upper.FroudeNumber)
            {
                // Linear interpolation
                var fnRange = upper.FroudeNumber - lower.FroudeNumber;
                var cbRange = lower.MaxEfficientCb - upper.MaxEfficientCb; // Note: Cb decreases as Fn increases

                if (fnRange == 0)
                {
                    return lower.MaxEfficientCb;
                }

                var t = (froudeNumber - lower.FroudeNumber) / fnRange;
                var interpolatedCb = lower.MaxEfficientCb - (cbRange * t);

                return interpolatedCb;
            }
        }

        // Fallback (should not reach here)
        return ReferencePoints[^1].MaxEfficientCb;
    }

    /// <summary>
    /// Checks if a design violates the Alexander Limit (Cb exceeds max efficient for given Fn).
    /// </summary>
    /// <param name="froudeNumber">Froude Number of the design</param>
    /// <param name="blockCoefficient">Block Coefficient of the design</param>
    /// <returns>True if the design violates the limit (Cb > max efficient Cb)</returns>
    public static bool ViolatesLimit(decimal froudeNumber, decimal blockCoefficient)
    {
        var maxEfficientCb = InterpolateMaxEfficientCb(froudeNumber);
        return blockCoefficient > maxEfficientCb;
    }

    /// <summary>
    /// Calculates the margin to the Alexander Limit as a percentage.
    ///
    /// Positive margin: Design is below the limit (safe, efficient)
    /// Zero margin: Design is exactly at the limit
    /// Negative margin: Design violates the limit (inefficient or impossible)
    /// </summary>
    /// <param name="froudeNumber">Froude Number of the design</param>
    /// <param name="blockCoefficient">Block Coefficient of the design</param>
    /// <returns>Margin percentage: (MaxCb - ActualCb) / MaxCb * 100</returns>
    public static decimal CalculateMarginPercent(decimal froudeNumber, decimal blockCoefficient)
    {
        var maxEfficientCb = InterpolateMaxEfficientCb(froudeNumber);

        if (maxEfficientCb == 0)
        {
            return 0;
        }

        var margin = (maxEfficientCb - blockCoefficient) / maxEfficientCb * 100m;
        return margin;
    }

    /// <summary>
    /// Determines the validation severity level based on margin to limit.
    /// </summary>
    /// <param name="froudeNumber">Froude Number of the design</param>
    /// <param name="blockCoefficient">Block Coefficient of the design</param>
    /// <returns>Severity level: "Error" if violated, "Warning" if within 5%, "Info" if safe</returns>
    public static string GetSeverityLevel(decimal froudeNumber, decimal blockCoefficient)
    {
        var marginPercent = CalculateMarginPercent(froudeNumber, blockCoefficient);

        if (marginPercent < 0)
        {
            return "Error"; // Violates limit
        }
        else if (marginPercent < 5)
        {
            return "Warning"; // Approaching limit (< 5% margin)
        }
        else
        {
            return "Info"; // Safe zone
        }
    }

    /// <summary>
    /// Gets the closest reference point for a given Froude Number (for display/reference purposes).
    /// </summary>
    /// <param name="froudeNumber">Froude Number to find closest point for</param>
    /// <returns>Closest reference point</returns>
    public static AlexanderLimitPoint GetClosestReferencePoint(decimal froudeNumber)
    {
        AlexanderLimitPoint? closest = null;
        decimal minDistance = decimal.MaxValue;

        foreach (var point in ReferencePoints)
        {
            var distance = Math.Abs(point.FroudeNumber - froudeNumber);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = point;
            }
        }

        return closest ?? ReferencePoints[0];
    }
}
