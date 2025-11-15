namespace DataService.Tests.TestData;

    /// <summary>
    /// Barge validation reference data as constants
    /// Analytical validation data for rectangular barge (L=100m, B=20m)
    /// Source: Analytical calculations
    ///
    /// NOTE: Reference data corrected to match 100m x 20m barge dimensions
    /// Volume = L * B * T = 100 * 20 * T = 2000 * T
    /// KB = T/2 (for rectangular barge)
    /// BMt = (B²/12) / T = (400/12) / T = 33.333... / T
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
            Volume_disp_m3 = 1000.0m, // 100 * 20 * 0.5
            Weight_tonnes = 1025.0m, // 1000 * 1.025
            KB_m = 0.25m, // T/2
            BM_T_m = 66.666666666666666666666666667m, // (B²/12)/T = (400/12)/0.5 = 33.333/0.5 = 66.667
            KM_T_m = 66.916666666666666666666666667m, // KB + BMt
            GM_T_m = 66.416666666666666666666666667m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 1.0m,
            Volume_disp_m3 = 2000.0m, // 100 * 20 * 1.0
            Weight_tonnes = 2050.0m, // 2000 * 1.025
            KB_m = 0.5m, // T/2
            BM_T_m = 33.333333333333333333333333333m, // (400/12)/1.0 = 33.333
            KM_T_m = 33.833333333333333333333333333m, // KB + BMt
            GM_T_m = 33.333333333333333333333333333m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 2.0m,
            Volume_disp_m3 = 4000.0m, // 100 * 20 * 2.0
            Weight_tonnes = 4100.0m, // 4000 * 1.025
            KB_m = 1.0m, // T/2
            BM_T_m = 16.666666666666666666666666667m, // (400/12)/2.0 = 33.333/2 = 16.667
            KM_T_m = 17.666666666666666666666666667m, // KB + BMt
            GM_T_m = 17.166666666666666666666666667m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 3.0m,
            Volume_disp_m3 = 6000.0m, // 100 * 20 * 3.0
            Weight_tonnes = 6150.0m, // 6000 * 1.025
            KB_m = 1.5m, // T/2
            BM_T_m = 11.111111111111111111111111111m, // (400/12)/3.0 = 33.333/3 = 11.111
            KM_T_m = 12.611111111111111111111111111m, // KB + BMt
            GM_T_m = 12.111111111111111111111111111m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 4.0m,
            Volume_disp_m3 = 8000.0m, // 100 * 20 * 4.0
            Weight_tonnes = 8200.0m, // 8000 * 1.025
            KB_m = 2.0m, // T/2
            BM_T_m = 8.333333333333333333333333333m, // (400/12)/4.0 = 33.333/4 = 8.333
            KM_T_m = 10.333333333333333333333333333m, // KB + BMt
            GM_T_m = 9.833333333333333333333333333m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 5.0m,
            Volume_disp_m3 = 10000.0m, // 100 * 20 * 5.0
            Weight_tonnes = 10250.0m, // 10000 * 1.025
            KB_m = 2.5m, // T/2
            BM_T_m = 6.666666666666666666666666667m, // (400/12)/5.0 = 33.333/5 = 6.667
            KM_T_m = 9.166666666666666666666666667m, // KB + BMt
            GM_T_m = 8.666666666666666666666666667m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 6.0m,
            Volume_disp_m3 = 12000.0m, // 100 * 20 * 6.0
            Weight_tonnes = 12300.0m, // 12000 * 1.025
            KB_m = 3.0m, // T/2
            BM_T_m = 5.555555555555555555555555556m, // (400/12)/6.0 = 33.333/6 = 5.556
            KM_T_m = 8.555555555555555555555555556m, // KB + BMt
            GM_T_m = 8.055555555555555555555555556m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 7.0m,
            Volume_disp_m3 = 14000.0m, // 100 * 20 * 7.0
            Weight_tonnes = 14350.0m, // 14000 * 1.025
            KB_m = 3.5m, // T/2
            BM_T_m = 4.761904761904761904761904762m, // (400/12)/7.0 = 33.333/7 = 4.762
            KM_T_m = 8.261904761904761904761904762m, // KB + BMt
            GM_T_m = 7.761904761904761904761904762m // KMt - KG (assuming KG = 0.5m)
        },
        new BargeHydroTableRecord
        {
            Draft_T_m = 8.0m,
            Volume_disp_m3 = 16000.0m, // 100 * 20 * 8.0
            Weight_tonnes = 16400.0m, // 16000 * 1.025
            KB_m = 4.0m, // T/2
            BM_T_m = 4.166666666666666666666666667m, // (400/12)/8.0 = 33.333/8 = 4.167
            KM_T_m = 8.166666666666666666666666667m, // KB + BMt
            GM_T_m = 7.666666666666666666666666667m // KMt - KG (assuming KG = 0.5m)
        }
    };
}
