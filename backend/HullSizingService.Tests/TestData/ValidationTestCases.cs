namespace HullSizingService.Tests.TestData;

/// <summary>
/// Validation test cases from Ship Design Validation Handbook
/// Single source of truth for all validation test cases used in hull sizing validation.
///
/// Source: Ship Design Validation Handbook
/// These test cases validate hull sizing results against real-world vessel scenarios.
/// </summary>
public static class ValidationTestCases
{
    /// <summary>
    /// Calibration Case: 40,000 DWT Product Carrier
    ///
    /// This is the "Gold Standard" calibration case used to validate the solver's accuracy
    /// before running general test cases. This case has known, validated results.
    ///
    /// Source: Ship Design Validation Handbook - Calibration Case
    /// </summary>
    public static class CalibrationCase
    {
        /// <summary>Vessel type/category</summary>
        public const string VesselType = "commercial";
        public const string VesselSubtype = "product_carrier";

        /// <summary>Primary cargo capacity requirement</summary>
        public const decimal DeadweightTonnes = 40000m;

        /// <summary>Constraint on Length Between Perpendiculars (meters)</summary>
        public const decimal LppM = 185.0m;

        /// <summary>Constraint on Width (meters)</summary>
        public const decimal BeamM = 28.0m;

        /// <summary>Constraint on Depth Submerged (meters)</summary>
        public const decimal DraftM = 12.87m;

        /// <summary>Service speed used for resistance calculations (knots)</summary>
        public const decimal ServiceSpeedKn = 14.0m;

        /// <summary>Expected Block Coefficient range for 14-knot hull</summary>
        public const decimal ExpectedCbMin = 0.792m;
        public const decimal ExpectedCbMax = 0.80m;
        public const decimal ExpectedCbMean = 0.796m;

        /// <summary>Expected Midship Coefficient - should be nearly rectangular</summary>
        public const decimal ExpectedCm = 0.99m;
        public const decimal ExpectedCmTolerance = 0.01m; // ±0.01

        /// <summary>Expected Cb tolerance for calibration case (tight, known reference)</summary>
        public const decimal ExpectedCbTolerance = 0.02m; // ±0.02

        // Additional parameters from prefinal_1 document
        /// <summary>Expected Waterplane Coefficient (from prefinal_1: finalized CW = 0.87)</summary>
        public const decimal ExpectedCwp = 0.87m;
        public const decimal ExpectedCwpTolerance = 0.02m; // ±0.02

        /// <summary>Expected Depth (from prefinal_1: finalized DEPTH = 16.40 m)</summary>
        public const decimal ExpectedDepthM = 16.40m;

        /// <summary>Expected Freeboard (from prefinal_1: FINAL FREEBOARD = 3.55 m)</summary>
        public const decimal ExpectedFreeboardM = 3.55m;

        /// <summary>Expected Wetted Surface Area (from prefinal_1: 8437.85 + 2% = 8606.61 m²)</summary>
        public const decimal ExpectedWettedSurfaceM2 = 8606.61m;

        /// <summary>Hull family specifications</summary>
        public const string BowFamily = "bulbous_bow";
        public const string MidshipFamily = "full_midship";
        public const string SternFamily = "transom_stern";

        /// <summary>Visual validation characteristics</summary>
        public const string VisualDescription =
            "Midship section must look almost perfectly rectangular. " +
            "Very long parallel midbody (straight sides) characteristic of high Cb vessel.";
    }

    /// <summary>
    /// Test Case A: Bulk Carrier/VLCC
    ///
    /// Validates the solver across low-speed, high-block-coefficient regime.
    /// Represents vessels optimized for maximum cargo volume at moderate speeds.
    ///
    /// Source: Ship Design Validation Handbook - Standard Test Cases
    /// </summary>
    public static class TestCaseA
    {
        public const string TestId = "TC-A";
        public const string VesselType = "commercial";
        public const string VesselSubtype = "bulk_carrier";
        public const string Name = "Bulk Carrier (VLCC)";

        /// <summary>Cargo capacity (tonnes)</summary>
        public const decimal CargoTonnes = 250000m;

        /// <summary>Service speed (knots)</summary>
        public const decimal ServiceSpeedKn = 15.0m;

        /// <summary>Expected Block Coefficient - high Cb for volume optimization</summary>
        public const decimal ExpectedCbMin = 0.82m;
        public const decimal ExpectedCbMax = 0.86m;
        public const decimal ExpectedCbMean = 0.84m;
        public const decimal ExpectedCbTolerance = 0.05m; // ±0.05

        /// <summary>Expected Froude Number range</summary>
        public const decimal ExpectedFnMin = 0.13m;
        public const decimal ExpectedFnMax = 0.15m;
        public const decimal ExpectedFnTolerance = 0.01m;

        /// <summary>Expected length range (meters)</summary>
        public const decimal ExpectedLppMin = 320m;
        public const decimal ExpectedLppMax = 340m;

        /// <summary>Expected beam range (meters)</summary>
        public const decimal ExpectedBeamMin = 58m;
        public const decimal ExpectedBeamMax = 60m;

        /// <summary>EHP trend: Low (primarily frictional drag)</summary>
        public const string ExpectedEhpTrend = "Low";

        /// <summary>Hull family specifications</summary>
        public const string BowFamily = "blunt_bow"; // or "cylindrical_bow"
        public const string MidshipFamily = "full_midship"; // Maximum volume
        public const string SternFamily = "transom_stern";

        /// <summary>Visual validation: Hull lines should be bluff (blunt) at bow. Large, continuous parallel midbody.</summary>
        public const string VisualDescription =
            "Hull lines should be bluff (blunt) at the bow. Large, continuous parallel midbody.";
    }

    /// <summary>
    /// Test Case B: General Cargo
    ///
    /// Validates the solver across medium-speed, medium-block-coefficient regime.
    /// Represents a balanced design between speed and cargo capacity.
    ///
    /// Source: Ship Design Validation Handbook - Standard Test Cases
    /// This corresponds to the "General Cargo, Slower Speed Optimization" case.
    /// </summary>
    public static class TestCaseB
    {
        public const string TestId = "TC-B";
        public const string VesselType = "commercial";
        public const string VesselSubtype = "general_cargo";
        public const string Name = "General Cargo";

        /// <summary>Cargo capacity (tonnes)</summary>
        public const decimal CargoTonnes = 50000m;

        /// <summary>Service speed (knots)</summary>
        public const decimal ServiceSpeedKn = 20.0m;

        /// <summary>Expected Block Coefficient - moderate Cb</summary>
        public const decimal ExpectedCbMin = 0.60m;
        public const decimal ExpectedCbMax = 0.70m;
        public const decimal ExpectedCbMean = 0.65m;
        public const decimal ExpectedCbTolerance = 0.05m; // ±0.05

        /// <summary>Expected Froude Number range</summary>
        public const decimal ExpectedFnMin = 0.20m;
        public const decimal ExpectedFnMax = 0.25m;
        public const decimal ExpectedFnTolerance = 0.01m;

        /// <summary>Expected length range (meters)</summary>
        public const decimal ExpectedLppMin = 190m;
        public const decimal ExpectedLppMax = 210m;

        /// <summary>Expected beam range (meters)</summary>
        public const decimal ExpectedBeamMin = 30m;
        public const decimal ExpectedBeamMax = 32m;

        /// <summary>EHP trend: Moderate (transitioning to wave drag)</summary>
        public const string ExpectedEhpTrend = "Moderate";

        /// <summary>Hull family specifications</summary>
        public const string BowFamily = "bulbous_bow";
        public const string MidshipFamily = "medium_midship"; // or "full_midship" for slower speed variant
        public const string SternFamily = "transom_stern";

        /// <summary>Visual validation: Balanced design</summary>
        public const string VisualDescription =
            "Balanced design between volume and speed efficiency.";
    }

    /// <summary>
    /// Test Case B Variant: General Cargo at 12 knots
    ///
    /// This is the slower speed optimization variant mentioned in the validation handbook.
    /// At 12 knots, the design should prioritize fuller hull form for maximum volume.
    ///
    /// Source: Ship Design Validation Handbook - General Cargo, Slower Speed Optimization
    /// </summary>
    public static class TestCaseB_Slow
    {
        public const string TestId = "TC-B-Slow";
        public const string VesselType = "commercial";
        public const string VesselSubtype = "general_cargo";
        public const string Name = "General Cargo (Slow Speed)";

        /// <summary>Cargo capacity (tonnes)</summary>
        public const decimal CargoTonnes = 50000m;

        /// <summary>Service speed (knots) - low, efficient speed</summary>
        public const decimal ServiceSpeedKn = 12.0m;

        /// <summary>Expected Block Coefficient - much higher for low speed</summary>
        public const decimal ExpectedCbMin = 0.78m;
        public const decimal ExpectedCbMax = 0.82m;
        public const decimal ExpectedCbMean = 0.80m;
        public const decimal ExpectedCbTolerance = 0.05m; // ±0.05

        /// <summary>Expected Froude Number range - low Fn</summary>
        public const decimal ExpectedFnMin = 0.14m;
        public const decimal ExpectedFnMax = 0.16m;
        public const decimal ExpectedFnTolerance = 0.01m;

        /// <summary>Expected length range (meters)</summary>
        public const decimal ExpectedLppMin = 190m;
        public const decimal ExpectedLppMax = 210m;

        /// <summary>Expected beam range (meters)</summary>
        public const decimal ExpectedBeamMin = 30m;
        public const decimal ExpectedBeamMax = 32m;

        /// <summary>EHP trend: Low (mostly viscous resistance)</summary>
        public const string ExpectedEhpTrend = "Low";

        /// <summary>Hull family specifications - full midship for maximum volume</summary>
        public const string BowFamily = "bulbous_bow";
        public const string MidshipFamily = "full_midship"; // Critical: Full form ideal for low speed
        public const string SternFamily = "transom_stern";

        /// <summary>Visual validation: Blunter entry. Long, straight parallel midbody (box-like).</summary>
        public const string VisualDescription =
            "Blunter entry. Long, straight parallel midbody (box-like). " +
            "Waterlines will appear much straighter for a greater portion of the vessel's length.";
    }

    /// <summary>
    /// Test Case C: Fast Container Ship
    ///
    /// Validates the solver across high-speed, low-block-coefficient regime.
    /// Represents vessels optimized for speed with fine hull forms.
    ///
    /// Source: Ship Design Validation Handbook - Standard Test Cases
    /// </summary>
    public static class TestCaseC
    {
        public const string TestId = "TC-C";
        public const string VesselType = "commercial";
        public const string VesselSubtype = "container_ship";
        public const string Name = "Fast Container Ship";

        /// <summary>Cargo capacity (tonnes)</summary>
        public const decimal CargoTonnes = 10000m;

        /// <summary>Service speed (knots)</summary>
        public const decimal ServiceSpeedKn = 25.0m;

        /// <summary>Expected Block Coefficient - low Cb for speed optimization</summary>
        public const decimal ExpectedCbMin = 0.50m;
        public const decimal ExpectedCbMax = 0.65m;
        public const decimal ExpectedCbMean = 0.57m;
        public const decimal ExpectedCbTolerance = 0.05m; // ±0.05

        /// <summary>Expected Froude Number range - high Fn</summary>
        public const decimal ExpectedFnMin = 0.30m;
        public const decimal ExpectedFnMax = 0.35m;
        public const decimal ExpectedFnTolerance = 0.01m;

        /// <summary>Expected length range (meters)</summary>
        public const decimal ExpectedLppMin = 250m;
        public const decimal ExpectedLppMax = 290m;

        /// <summary>Expected beam range (meters)</summary>
        public const decimal ExpectedBeamMin = 32m;
        public const decimal ExpectedBeamMax = 40m;

        /// <summary>EHP trend: High (primarily wave drag)</summary>
        public const string ExpectedEhpTrend = "High";

        /// <summary>Hull family specifications</summary>
        public const string BowFamily = "fine_bow"; // or "axe_bow"
        public const string MidshipFamily = "fine_midship"; // Minimum volume for speed
        public const string SternFamily = "transom_stern";

        /// <summary>Visual validation: Fine entry (sharp angle), straight run aft. No long parallel midbody.</summary>
        public const string VisualDescription =
            "Hull lines must show a sharp entry angle at the bow. " +
            "No visible parallel midbody. The lines will look more like a tear drop than a box.";
    }

    /// <summary>
    /// Helper method to get all test case IDs
    /// </summary>
    public static string[] GetAllTestCaseIds()
    {
        return new[]
        {
            "CalibrationCase",
            TestCaseA.TestId,
            TestCaseB.TestId,
            TestCaseB_Slow.TestId,
            TestCaseC.TestId
        };
    }
}
