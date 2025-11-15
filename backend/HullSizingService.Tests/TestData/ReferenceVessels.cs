namespace HullSizingService.Tests.TestData;

/// <summary>
/// Reference vessel data for validation testing
/// Sources: SIMMAN (KCS), KRISO (KVLCC2), ITTC (Series 60)
/// </summary>
public static class ReferenceVessels
{
    /// <summary>
    /// KCS (KRISO Container Ship) - SIMMAN 2008 benchmark
    /// </summary>
    public static class KCS
    {
        public const decimal LppM = 230.0m;
        public const decimal LwlM = 232.5m;
        public const decimal BeamM = 32.2m;
        public const decimal DraftM = 10.8m;
        public const decimal DepthM = 19.0m;
        public const decimal Cb = 0.651m;
        public const decimal Cp = 0.673m;
        public const decimal Cwp = 0.858m;
        public const decimal Cm = 0.977m;
        public const decimal DisplacementT = 52030m;
        public const decimal ServiceSpeedKn = 24.0m;
        public const decimal DesignFn = 0.260m;

        // Expected values for validation
        public const decimal ExpectedLOverB = 7.14m; // 230 / 32.2
        public const decimal ExpectedBOverT = 2.98m; // 32.2 / 10.8
        public const decimal ExpectedDOverT = 1.76m; // 19.0 / 10.8
    }

    /// <summary>
    /// KVLCC2 (KRISO Very Large Crude Carrier) - SIMMAN 2008 benchmark
    /// </summary>
    public static class KVLCC2
    {
        public const decimal LppM = 320.0m;
        public const decimal LwlM = 320.0m;
        public const decimal BeamM = 58.0m;
        public const decimal DraftM = 20.8m;
        public const decimal DepthM = 30.0m;
        public const decimal Cb = 0.8098m;
        public const decimal Cp = 0.8390m;
        public const decimal Cwp = 0.8960m;
        public const decimal Cm = 0.9950m;
        public const decimal DisplacementT = 312622m;
        public const decimal ServiceSpeedKn = 15.5m;
        public const decimal DesignFn = 0.142m;

        // Expected values
        public const decimal ExpectedLOverB = 5.52m; // 320 / 58
        public const decimal ExpectedBOverT = 2.79m; // 58 / 20.8
        public const decimal ExpectedDOverT = 1.44m; // 30 / 20.8
    }

    /// <summary>
    /// Series 60 (Cb=0.70) - ITTC benchmark
    /// </summary>
    public static class Series60_Cb070
    {
        public const decimal LppM = 150.0m;
        public const decimal BeamM = 21.43m;
        public const decimal DraftM = 8.57m;
        public const decimal Cb = 0.700m;
        public const decimal Cp = 0.714m;
        public const decimal Cwp = 0.840m;
        public const decimal DisplacementT = 19500m;
        public const decimal ServiceSpeedKn = 16.0m;
        public const decimal DesignFn = 0.215m;

        // Expected values
        public const decimal ExpectedLOverB = 7.0m;
        public const decimal ExpectedBOverT = 2.5m;
    }

    /// <summary>
    /// Simple barge for analytical validation
    /// </summary>
    public static class Barge
    {
        public const decimal LppM = 50.0m;
        public const decimal BeamM = 10.0m;
        public const decimal DraftM = 3.0m;
        public const decimal DepthM = 4.0m;
        public const decimal Cb = 1.000m; // Perfect box
        public const decimal Cp = 1.000m;
        public const decimal Cwp = 1.000m;
        public const decimal Cm = 1.000m;
        public const decimal DisplacementT = 1537.5m; // 50 * 10 * 3 * 1.0 * 1.025
        public const decimal ServiceSpeedKn = 8.0m;

        // Analytical stability for box
        // Iwp = (1/12) * L * B³ = (1/12) * 50 * 1000 = 4166.67 m⁴
        // ∇ = 1500 m³
        // BMt = Iwp / ∇ = 4166.67 / 1500 = 2.78 m
        // KB = 0.5 * T = 1.5 m
        // KG ~ 2.5 m (estimate)
        // GMt = KB + BMt - KG = 1.5 + 2.78 - 2.5 = 1.78 m
        public const decimal ExpectedBMt = 2.78m;
        public const decimal ExpectedKB = 1.5m;
        public const decimal ExpectedGMt = 1.78m; // Approximate
    }
}















