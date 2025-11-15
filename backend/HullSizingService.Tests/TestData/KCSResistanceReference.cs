using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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
/// </summary>
public static class KCSResistanceReference
{
    /// <summary>
    /// Loads KCS resistance reference data from CSV
    /// CSV format: speed_mps, RT_ref_N, source
    /// </summary>
    public static List<KCSResistanceRecord> LoadResistanceData(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"KCS resistance reference CSV not found at {csvPath}");
        }

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        });

        csv.Context.RegisterClassMap<KCSResistanceRecordMap>();

        var records = csv.GetRecords<KCSResistanceRecord>().ToList();
        return records;
    }

    /// <summary>
    /// Gets the default CSV path (in TestData/validation/)
    /// </summary>
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

// CSV Class Map
internal sealed class KCSResistanceRecordMap : ClassMap<KCSResistanceRecord>
{
    public KCSResistanceRecordMap()
    {
        Map(m => m.Speed_mps).Name("speed_mps");
        Map(m => m.RT_ref_N).Name("RT_ref_N");
        Map(m => m.Source).Name("source");
    }
}
