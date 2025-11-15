namespace HullSizingService.Tests.TestData;

/// <summary>
/// KCS resistance reference data as constants
/// Synthetic test data for validation
///
/// UNITS: All values stored in SI base units (matches database schema)
/// - Speed: meters per second (m/s)
/// - Resistance: Newtons (N)
///
/// Unit conversion is handled at API boundaries via UnitConversionService
/// </summary>
public static class KCSResistanceConstants
{
    /// <summary>
    /// KCS resistance reference data (synthetic for testing)
    /// Speed vs Total Resistance
    /// Units: SI (Speed: m/s, Resistance: N)
    /// </summary>
    public static readonly KCSResistanceRecord[] ResistanceData = new[]
    {
        new KCSResistanceRecord
        {
            Speed_mps = 0.5m,
            RT_ref_N = 42.5m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 0.75m,
            RT_ref_N = 101.25m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 1.0m,
            RT_ref_N = 190.0m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 1.25m,
            RT_ref_N = 312.5m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 1.5m,
            RT_ref_N = 472.5m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 1.75m,
            RT_ref_N = 673.75m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 2.0m,
            RT_ref_N = 920.0m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 2.25m,
            RT_ref_N = 1215.0m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 2.5m,
            RT_ref_N = 1562.5m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 2.75m,
            RT_ref_N = 1966.25m,
            Source = "synthetic_for_harness"
        },
        new KCSResistanceRecord
        {
            Speed_mps = 3.0m,
            RT_ref_N = 2430.0m,
            Source = "synthetic_for_harness"
        }
    };
}
