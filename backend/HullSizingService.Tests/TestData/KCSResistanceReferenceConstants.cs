namespace HullSizingService.Tests.TestData;

/// <summary>
/// KCS resistance reference data constants
/// Converted from KCS_resistance_reference.csv for better maintainability and testability
/// Used for validating Holtrop-Mennen resistance calculations
/// </summary>
public static class KCSResistanceReferenceConstants
{
    /// <summary>
    /// Get all KCS resistance reference records
    /// </summary>
    public static List<KCSResistanceRecord> GetResistanceData()
    {
        return new List<KCSResistanceRecord>
        {
            new KCSResistanceRecord { Speed_mps = 0.5m, RT_ref_N = 42.5m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 0.75m, RT_ref_N = 101.25m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 1.0m, RT_ref_N = 190.0m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 1.25m, RT_ref_N = 312.5m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 1.5m, RT_ref_N = 472.5m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 1.75m, RT_ref_N = 673.75m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 2.0m, RT_ref_N = 920.0m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 2.25m, RT_ref_N = 1215.0m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 2.5m, RT_ref_N = 1562.5m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 2.75m, RT_ref_N = 1966.25m, Source = "synthetic_for_harness" },
            new KCSResistanceRecord { Speed_mps = 3.0m, RT_ref_N = 2430.0m, Source = "synthetic_for_harness" }
        };
    }
}

