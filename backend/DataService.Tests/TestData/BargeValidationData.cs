using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataService.Tests.TestData;

/// <summary>
/// Validation reference data for rectangular barge
/// Used for regression testing and validation
/// </summary>
public static class BargeValidationData
{
    /// <summary>
    /// Loads hydrostatic table reference data from CSV
    /// CSV format: Draft_T(m), Volume_disp_m3, Weight_tonnes, KB_m, BM_T_m, KM_T_m, GM_T_m
    /// </summary>
    public static List<BargeHydroTableRecord> LoadHydroTable(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"Barge hydro table CSV not found at {csvPath}");
        }

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        });

        csv.Context.RegisterClassMap<BargeHydroTableRecordMap>();

        var records = csv.GetRecords<BargeHydroTableRecord>().ToList();
        return records;
    }

    /// <summary>
    /// Loads GZ curve reference data from CSV
    /// CSV format: Angle_deg, GZ_m, GM_T_m
    /// </summary>
    public static List<BargeGZCurveRecord> LoadGZCurve(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"Barge GZ curve CSV not found at {csvPath}");
        }

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        });

        csv.Context.RegisterClassMap<BargeGZCurveRecordMap>();

        var records = csv.GetRecords<BargeGZCurveRecord>().ToList();
        return records;
    }

    /// <summary>
    /// Gets the default CSV paths (in TestData/validation/)
    /// </summary>
    public static string GetHydroTablePath()
    {
        var basePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "validation",
            "Barge_Hydro_Table.csv");

        // Fallback for different test execution paths
        if (!File.Exists(basePath))
        {
            basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "TestData",
                "validation",
                "Barge_Hydro_Table.csv");
        }

        return basePath;
    }

    public static string GetGZCurvePath()
    {
        var basePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "validation",
            "Barge_GZ_Curve.csv");

        // Fallback for different test execution paths
        if (!File.Exists(basePath))
        {
            basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "TestData",
                "validation",
                "Barge_GZ_Curve.csv");
        }

        return basePath;
    }
}

/// <summary>
/// Barge hydrostatic table reference record
/// </summary>
public record BargeHydroTableRecord
{
    public decimal Draft_T_m { get; init; }
    public decimal Volume_disp_m3 { get; init; }
    public decimal Weight_tonnes { get; init; }
    public decimal KB_m { get; init; }
    public decimal BM_T_m { get; init; }
    public decimal KM_T_m { get; init; }
    public decimal GM_T_m { get; init; }
}

/// <summary>
/// Barge GZ curve reference record
/// </summary>
public record BargeGZCurveRecord
{
    public decimal Angle_deg { get; init; }
    public decimal GZ_m { get; init; }
    public decimal GM_T_m { get; init; }
}

// CSV Class Maps
internal sealed class BargeHydroTableRecordMap : ClassMap<BargeHydroTableRecord>
{
    public BargeHydroTableRecordMap()
    {
        Map(m => m.Draft_T_m).Name("Draft_T(m)");
        Map(m => m.Volume_disp_m3).Name("Volume_disp_m3");
        Map(m => m.Weight_tonnes).Name("Weight_tonnes");
        Map(m => m.KB_m).Name("KB_m");
        Map(m => m.BM_T_m).Name("BM_T_m");
        Map(m => m.KM_T_m).Name("KM_T_m");
        Map(m => m.GM_T_m).Name("GM_T_m");
    }
}

internal sealed class BargeGZCurveRecordMap : ClassMap<BargeGZCurveRecord>
{
    public BargeGZCurveRecordMap()
    {
        Map(m => m.Angle_deg).Name("Angle_deg");
        Map(m => m.GZ_m).Name("GZ_m");
        Map(m => m.GM_T_m).Name("GM_T_m");
    }
}
