namespace DataService.Tests.TestData;

/// <summary>
/// Analytical solutions for rectangular barge stability
/// Used for validating stability calculations
/// </summary>
public static class BargeGZReference
{
    /// <summary>
    /// Computes analytical GZ for a rectangular barge using exact formula
    /// GZ = (B²/(12*T)) * sin(φ) * cos(φ) for wall-sided rectangular section
    /// Valid for all angles where vessel remains stable
    /// </summary>
    /// <param name="beam">Beam (m)</param>
    /// <param name="draft">Draft (m)</param>
    /// <param name="heelAngleDeg">Heel angle (degrees)</param>
    /// <returns>GZ in meters</returns>
    public static decimal ComputeAnalyticalGZ(decimal beam, decimal draft, decimal heelAngleDeg)
    {
        // Convert angle to radians
        var angleRad = (double)heelAngleDeg * Math.PI / 180.0;

        // Analytical formula for rectangular barge
        // GZ = (B²/(12*T)) * sin(φ) * cos(φ)
        var gz = (beam * beam / (12m * draft)) * (decimal)Math.Sin(angleRad) * (decimal)Math.Cos(angleRad);

        return gz;
    }

    /// <summary>
    /// Computes initial GMT for rectangular barge
    /// GMT = (B²/(12*T)) - KG
    /// </summary>
    public static decimal ComputeGMT(decimal beam, decimal draft, decimal kg)
    {
        var bmt = beam * beam / (12m * draft);
        var kb = draft / 2m; // For rectangular barge
        var kmt = kb + bmt;
        var gmt = kmt - kg;
        return gmt;
    }

    /// <summary>
    /// Gets expected max GZ angle for rectangular barge (45° theoretically)
    /// </summary>
    public static decimal GetExpectedMaxGZAngle()
    {
        return 45m; // degrees
    }

    /// <summary>
    /// Computes area under GZ curve from 0 to specified angle using analytical formula
    /// Integral of GZ = Integral of (B²/(12*T)) * sin(φ) * cos(φ) dφ
    /// = (B²/(12*T)) * (sin²(φ)/2)
    /// </summary>
    public static decimal ComputeAreaUnderCurve(decimal beam, decimal draft, decimal fromAngleDeg, decimal toAngleDeg)
    {
        var fromRad = (double)fromAngleDeg * Math.PI / 180.0;
        var toRad = (double)toAngleDeg * Math.PI / 180.0;

        // Integral: (B²/(12*T)) * [sin²(φ)/2] evaluated from fromAngle to toAngle
        var coefficient = beam * beam / (12m * draft);
        var sin2To = Math.Sin(toRad) * Math.Sin(toRad);
        var sin2From = Math.Sin(fromRad) * Math.Sin(fromRad);

        var area = coefficient * (decimal)((sin2To - sin2From) / 2.0);

        return area;
    }

    /// <summary>
    /// Test case: Standard barge (20m beam, 5m draft)
    /// </summary>
    public static (decimal beam, decimal draft, decimal kg) GetStandardBarge()
    {
        return (20m, 5m, 2.5m); // Beam, Draft, KG at centroid
    }

    /// <summary>
    /// Test case: Stable barge (low KG)
    /// </summary>
    public static (decimal beam, decimal draft, decimal kg) GetStableBarge()
    {
        return (20m, 5m, 2.0m); // Low KG → high GMT
    }

    /// <summary>
    /// Test case: Unstable barge (high KG)
    /// </summary>
    public static (decimal beam, decimal draft, decimal kg) GetUnstableBarge()
    {
        return (20m, 5m, 7.0m); // High KG → negative GMT
    }
}

