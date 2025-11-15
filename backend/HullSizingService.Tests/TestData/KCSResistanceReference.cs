namespace HullSizingService.Tests.TestData;

/// <summary>
/// KCS resistance reference record
/// </summary>
public record KCSResistanceRecord
{
    public decimal Speed_mps { get; init; }
    public decimal RT_ref_N { get; init; }
    public string Source { get; init; } = string.Empty;
}

/// <summary>
/// KCS resistance reference data for validation
/// Used for validating Holtrop-Mennen resistance calculations
/// Now uses constants instead of CSV files for better maintainability
/// </summary>
public static class KCSResistanceReference
{
    /// <summary>
    /// Loads KCS resistance reference data from constants
    /// </summary>
    /// <param name="csvPath">Ignored for backward compatibility - data now comes from constants</param>
    public static List<KCSResistanceRecord> LoadResistanceData(string? csvPath = null)
    {
        // Use constants instead of CSV file
        return KCSResistanceReferenceConstants.GetResistanceData();
    }

    /// <summary>
    /// Gets the default CSV path (deprecated - data now comes from constants)
    /// Kept for backward compatibility
    /// </summary>
    [Obsolete("Data now comes from constants. Use LoadResistanceData() without parameters.")]
    public static string GetResistanceReferencePath()
    {
        var basePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "validation",
            "KCS_resistance_reference.csv");

        // Fallback for different test execution paths
        if (!File.Exists(basePath))
        {
            basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "TestData",
                "validation",
                "KCS_resistance_reference.csv");
        }

        return basePath;
    }
}
