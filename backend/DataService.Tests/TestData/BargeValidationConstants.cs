namespace DataService.Tests.TestData;

/// <summary>
/// Barge validation reference data as constants
/// Analytical validation data for rectangular barge (L=100m, B=20m)
/// Source: Analytical calculations
///
/// UNITS: All values stored in SI base units (matches database schema)
/// - Draft: meters (m)
/// - Volume: cubic meters (m³)
/// - Weight: metric tonnes
/// - KB, BMt, KMt, GMt: meters (m)
///
/// Unit conversion is handled at API boundaries via UnitConversionService
/// </summary>
public static class BargeValidationConstants
{
    /// <summary>
    /// Hydrostatic table reference data for rectangular barge
    /// Draft vs Volume, Weight, KB, BMt, KMt, GMt
    /// Units: SI (Draft: m, Volume: m³, Weight: tonnes, KB/BMt/KMt/GMt: m)
    /// </summary>
    public static readonly BargeHydroTableRecord[] HydroTable = new[]
    {
        new BargeHydroTableRecord
        {
            Draft_T_m = 0.5m,
            Volume_disp_m3 = 750.0m,
            Weight_tonnes = 768.75m,
            KB_m = 0.25m,
            BM_T_m = 37.5m,
            KM_T_m = 37.75m,
            GM_T_m = 31.75m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 1.0m,
            Volume_disp_m3 = 1500.0m,
            Weight_tonnes = 1537.5m,
            KB_m = 0.5m,
            BM_T_m = 18.75m,
            KM_T_m = 19.25m,
            GM_T_m = 13.25m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 2.0m,
            Volume_disp_m3 = 3000.0m,
            Weight_tonnes = 3075.0m,
            KB_m = 1.0m,
            BM_T_m = 9.375m,
            KM_T_m = 10.375m,
            GM_T_m = 4.375m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 3.0m,
            Volume_disp_m3 = 4500.0m,
            Weight_tonnes = 4612.5m,
            KB_m = 1.5m,
            BM_T_m = 6.25m,
            KM_T_m = 7.75m,
            GM_T_m = 1.75m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 4.0m,
            Volume_disp_m3 = 6000.0m,
            Weight_tonnes = 6150.0m,
            KB_m = 2.0m,
            BM_T_m = 4.6875m,
            KM_T_m = 6.6875m,
            GM_T_m = 0.6875m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 5.0m,
            Volume_disp_m3 = 7500.0m,
            Weight_tonnes = 7687.5m,
            KB_m = 2.5m,
            BM_T_m = 3.75m,
            KM_T_m = 6.25m,
            GM_T_m = 0.25m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 6.0m,
            Volume_disp_m3 = 9000.0m,
            Weight_tonnes = 9225.0m,
            KB_m = 3.0m,
            BM_T_m = 3.125m,
            KM_T_m = 6.125m,
            GM_T_m = 0.125m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 7.0m,
            Volume_disp_m3 = 10500.0m,
            Weight_tonnes = 10762.5m,
            KB_m = 3.5m,
            BM_T_m = 2.6785714285714284m,
            KM_T_m = 6.178571428571429m,
            GM_T_m = 0.17857142857142883m
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 8.0m,
            Volume_disp_m3 = 12000.0m,
            Weight_tonnes = 12300.0m,
            KB_m = 4.0m,
            BM_T_m = 2.34375m,
            KM_T_m = 6.34375m,
            GM_T_m = 0.34375m
        }
    };
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
