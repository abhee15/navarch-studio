using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of stability criteria checker per IMO A.749(18)
/// </summary>
public class StabilityCriteriaChecker : IStabilityCriteriaChecker
{
    private readonly ILogger<StabilityCriteriaChecker> _logger;

    // IMO A.749 basic intact stability criteria constants
    private const decimal REQUIRED_AREA_0_30_MRAD = 0.055m; // m·rad
    private const decimal REQUIRED_AREA_0_40_MRAD = 0.090m; // m·rad
    private const decimal REQUIRED_AREA_30_40_MRAD = 0.030m; // m·rad
    private const decimal REQUIRED_MAX_GZ_ANGLE_DEG = 25m; // degrees
    private const decimal REQUIRED_INITIAL_GMT_M = 0.15m; // meters
    private const decimal REQUIRED_GZ_AT_30_M = 0.20m; // meters

    // Conversion factors
    private const decimal RAD_TO_DEG = 57.29577951308232m; // 180/π
    private const decimal DEG_TO_RAD = 0.017453292519943295m; // π/180

    public StabilityCriteriaChecker(ILogger<StabilityCriteriaChecker> logger)
    {
        _logger = logger;
    }

    public StabilityCriteriaResultDto CheckIntactStabilityCriteria(StabilityCurveDto curve)
    {
        var criteria = new List<StabilityCriterionDto>();

        // Criterion 1: Area under GZ curve 0-30° ≥ 0.055 m·rad
        var area_0_30 = CalculateAreaUnderCurve(curve.Points, 0m, 30m);
        criteria.Add(new StabilityCriterionDto
        {
            Name = "Area under GZ curve (0° to 30°)",
            RequiredValue = REQUIRED_AREA_0_30_MRAD,
            ActualValue = area_0_30,
            Unit = "m·rad",
            Passed = area_0_30 >= REQUIRED_AREA_0_30_MRAD,
            Notes = $"Equivalent to {area_0_30 * RAD_TO_DEG:F3} m·deg"
        });

        // Criterion 2: Area under GZ curve 0-40° ≥ 0.090 m·rad
        var area_0_40 = CalculateAreaUnderCurve(curve.Points, 0m, 40m);
        criteria.Add(new StabilityCriterionDto
        {
            Name = "Area under GZ curve (0° to 40°)",
            RequiredValue = REQUIRED_AREA_0_40_MRAD,
            ActualValue = area_0_40,
            Unit = "m·rad",
            Passed = area_0_40 >= REQUIRED_AREA_0_40_MRAD,
            Notes = $"Equivalent to {area_0_40 * RAD_TO_DEG:F3} m·deg"
        });

        // Criterion 3: Area under GZ curve 30-40° ≥ 0.030 m·rad
        var area_30_40 = CalculateAreaUnderCurve(curve.Points, 30m, 40m);
        criteria.Add(new StabilityCriterionDto
        {
            Name = "Area under GZ curve (30° to 40°)",
            RequiredValue = REQUIRED_AREA_30_40_MRAD,
            ActualValue = area_30_40,
            Unit = "m·rad",
            Passed = area_30_40 >= REQUIRED_AREA_30_40_MRAD,
            Notes = $"Equivalent to {area_30_40 * RAD_TO_DEG:F3} m·deg"
        });

        // Criterion 4: Angle of maximum GZ ≥ 25°
        var (maxGZ, angleAtMaxGZ) = FindMaxGZ(curve.Points);
        criteria.Add(new StabilityCriterionDto
        {
            Name = "Angle at maximum GZ",
            RequiredValue = REQUIRED_MAX_GZ_ANGLE_DEG,
            ActualValue = angleAtMaxGZ,
            Unit = "degrees",
            Passed = angleAtMaxGZ >= REQUIRED_MAX_GZ_ANGLE_DEG,
            Notes = $"Maximum GZ = {maxGZ:F3} m"
        });

        // Criterion 5: Initial GMT ≥ 0.15 m
        criteria.Add(new StabilityCriterionDto
        {
            Name = "Initial metacentric height (GMT)",
            RequiredValue = REQUIRED_INITIAL_GMT_M,
            ActualValue = curve.InitialGMT,
            Unit = "m",
            Passed = curve.InitialGMT >= REQUIRED_INITIAL_GMT_M,
            Notes = null
        });

        // Criterion 6: GZ at 30° ≥ 0.20 m
        var gzAt30 = InterpolateGZ(curve.Points, 30m);
        criteria.Add(new StabilityCriterionDto
        {
            Name = "Righting arm at 30° heel",
            RequiredValue = REQUIRED_GZ_AT_30_M,
            ActualValue = gzAt30,
            Unit = "m",
            Passed = gzAt30 >= REQUIRED_GZ_AT_30_M,
            Notes = null
        });

        // Determine overall pass/fail
        bool allPassed = criteria.All(c => c.Passed);
        int passedCount = criteria.Count(c => c.Passed);
        int totalCount = criteria.Count;

        string summary = allPassed
            ? $"All {totalCount} IMO A.749(18) intact stability criteria satisfied."
            : $"Warning: {totalCount - passedCount} of {totalCount} criteria not satisfied. Vessel may not meet intact stability requirements.";

        _logger.LogInformation(
            "Stability criteria check completed: {PassedCount}/{TotalCount} passed",
            passedCount, totalCount);

        return new StabilityCriteriaResultDto
        {
            AllCriteriaPassed = allPassed,
            Criteria = criteria,
            Standard = "IMO A.749(18)",
            Summary = summary
        };
    }

    public decimal CalculateAreaUnderCurve(List<StabilityPointDto> points, decimal fromAngle, decimal toAngle)
    {
        if (points == null || points.Count < 2)
        {
            return 0m;
        }

        // Filter points in range
        var relevantPoints = points
            .Where(p => p.HeelAngle >= fromAngle && p.HeelAngle <= toAngle)
            .OrderBy(p => p.HeelAngle)
            .ToList();

        if (relevantPoints.Count < 2)
        {
            // Need to interpolate boundary points
            relevantPoints = new List<StabilityPointDto>();

            // Add interpolated point at fromAngle
            if (fromAngle > points.Min(p => p.HeelAngle))
            {
                relevantPoints.Add(new StabilityPointDto
                {
                    HeelAngle = fromAngle,
                    GZ = InterpolateGZ(points, fromAngle),
                    KN = 0m
                });
            }

            // Add all points in range
            relevantPoints.AddRange(points.Where(p => p.HeelAngle > fromAngle && p.HeelAngle < toAngle));

            // Add interpolated point at toAngle
            if (toAngle < points.Max(p => p.HeelAngle))
            {
                relevantPoints.Add(new StabilityPointDto
                {
                    HeelAngle = toAngle,
                    GZ = InterpolateGZ(points, toAngle),
                    KN = 0m
                });
            }

            relevantPoints = relevantPoints.OrderBy(p => p.HeelAngle).ToList();
        }

        if (relevantPoints.Count < 2)
        {
            return 0m;
        }

        // Trapezoidal integration
        // Convert angles to radians for integration (area in m·rad)
        decimal area = 0m;

        for (int i = 0; i < relevantPoints.Count - 1; i++)
        {
            var angle1 = relevantPoints[i].HeelAngle * DEG_TO_RAD;
            var angle2 = relevantPoints[i + 1].HeelAngle * DEG_TO_RAD;
            var gz1 = relevantPoints[i].GZ;
            var gz2 = relevantPoints[i + 1].GZ;

            // Trapezoidal rule: Area = 0.5 * (gz1 + gz2) * (angle2 - angle1)
            area += 0.5m * (gz1 + gz2) * (angle2 - angle1);
        }

        return area;
    }

    public (decimal maxGZ, decimal angleAtMaxGZ) FindMaxGZ(List<StabilityPointDto> points)
    {
        if (points == null || points.Count == 0)
        {
            return (0m, 0m);
        }

        var maxPoint = points.MaxBy(p => p.GZ);
        if (maxPoint == null)
        {
            return (0m, 0m);
        }

        return (maxPoint.GZ, maxPoint.HeelAngle);
    }

    public decimal InterpolateGZ(List<StabilityPointDto> points, decimal angle)
    {
        if (points == null || points.Count == 0)
        {
            return 0m;
        }

        // Find points surrounding the target angle
        var sortedPoints = points.OrderBy(p => p.HeelAngle).ToList();

        // Check if angle is at a data point
        var exactPoint = sortedPoints.FirstOrDefault(p => p.HeelAngle == angle);
        if (exactPoint != null)
        {
            return exactPoint.GZ;
        }

        // Check if angle is outside range
        if (angle < sortedPoints.First().HeelAngle)
        {
            return sortedPoints.First().GZ;
        }

        if (angle > sortedPoints.Last().HeelAngle)
        {
            return sortedPoints.Last().GZ;
        }

        // Find bounding points
        var lowerPoint = sortedPoints.LastOrDefault(p => p.HeelAngle < angle);
        var upperPoint = sortedPoints.FirstOrDefault(p => p.HeelAngle > angle);

        if (lowerPoint == null || upperPoint == null)
        {
            return 0m;
        }

        // Linear interpolation
        var fraction = (angle - lowerPoint.HeelAngle) / (upperPoint.HeelAngle - lowerPoint.HeelAngle);
        var interpolatedGZ = lowerPoint.GZ + fraction * (upperPoint.GZ - lowerPoint.GZ);

        return interpolatedGZ;
    }
}






