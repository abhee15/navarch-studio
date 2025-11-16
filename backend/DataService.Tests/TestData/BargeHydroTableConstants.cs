namespace DataService.Tests.TestData;

/// <summary>
/// Barge hydrostatic table constants for validation tests
/// Converted from Barge_Hydro_Table.csv for better maintainability and testability
/// </summary>
public static class BargeHydroTableConstants
{
    /// <summary>
    /// Barge hydrostatic table record
    /// </summary>
    public record BargeHydroRecord
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
    /// Get all barge hydrostatic table records
    /// </summary>
    public static List<BargeHydroRecord> GetHydroTable()
    {
        return new List<BargeHydroRecord>
        {
            new BargeHydroRecord
            {
                Draft_T_m = 0.5m,
                Volume_disp_m3 = 750.0m,
                Weight_tonnes = 768.7499999999999m,
                KB_m = 0.25m,
                BM_T_m = 37.5m,
                KM_T_m = 37.75m,
                GM_T_m = 31.75m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 1.0m,
                Volume_disp_m3 = 1500.0m,
                Weight_tonnes = 1537.4999999999998m,
                KB_m = 0.5m,
                BM_T_m = 18.75m,
                KM_T_m = 19.25m,
                GM_T_m = 13.25m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 2.0m,
                Volume_disp_m3 = 3000.0m,
                Weight_tonnes = 3074.9999999999995m,
                KB_m = 1.0m,
                BM_T_m = 9.375m,
                KM_T_m = 10.375m,
                GM_T_m = 4.375m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 3.0m,
                Volume_disp_m3 = 4500.0m,
                Weight_tonnes = 4612.5m,
                KB_m = 1.5m,
                BM_T_m = 6.25m,
                KM_T_m = 7.75m,
                GM_T_m = 1.75m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 4.0m,
                Volume_disp_m3 = 6000.0m,
                Weight_tonnes = 6149.999999999999m,
                KB_m = 2.0m,
                BM_T_m = 4.6875m,
                KM_T_m = 6.6875m,
                GM_T_m = 0.6875m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 5.0m,
                Volume_disp_m3 = 7500.0m,
                Weight_tonnes = 7687.499999999999m,
                KB_m = 2.5m,
                BM_T_m = 3.75m,
                KM_T_m = 6.25m,
                GM_T_m = 0.25m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 6.0m,
                Volume_disp_m3 = 9000.0m,
                Weight_tonnes = 9225.0m,
                KB_m = 3.0m,
                BM_T_m = 3.125m,
                KM_T_m = 6.125m,
                GM_T_m = 0.125m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 7.0m,
                Volume_disp_m3 = 10500.0m,
                Weight_tonnes = 10762.499999999998m,
                KB_m = 3.5m,
                BM_T_m = 2.6785714285714284m,
                KM_T_m = 6.178571428571429m,
                GM_T_m = 0.17857142857142883m
            },
            new BargeHydroRecord
            {
                Draft_T_m = 8.0m,
                Volume_disp_m3 = 12000.0m,
                Weight_tonnes = 12299.999999999998m,
                KB_m = 4.0m,
                BM_T_m = 2.34375m,
                KM_T_m = 6.34375m,
                GM_T_m = 0.34375m
            }
        };
    }
}
