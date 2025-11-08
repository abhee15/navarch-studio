using DataService.Services.Hydrostatics;
using DataService.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;
using Xunit.Abstractions;

namespace DataService.Tests.Validation;

/// <summary>
/// Validation tests using benchmark vessels with known analytical or published results
/// These tests ensure our calculations match reference data from literature/standards
/// </summary>
[Trait("Category", "Validation")]
public class BenchmarkValidationTests
{
    private readonly ITestOutputHelper _output;
    private readonly IntegrationEngine _integrationEngine;
    private readonly HydroCalculator _hydroCalculator;

    public BenchmarkValidationTests(ITestOutputHelper output)
    {
        _output = output;
        _integrationEngine = new IntegrationEngine(Mock.Of<ILogger<IntegrationEngine>>());
        _hydroCalculator = new HydroCalculator(_integrationEngine, Mock.Of<ILogger<HydroCalculator>>());
    }

    #region Rectangular Barge (Analytical Solution)

    [Fact]
    public void RectangularBarge_Displacement_MatchesAnalyticalSolution()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateRectangularBarge();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: Volume = Length × Breadth × Draft
        // = 100m × 20m × 5m = 10,000 m³
        var expectedDisplacement = 10000.0;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed Displacement: {result.Displacement:F2} m³");
        _output.WriteLine($"Expected Displacement: {expectedDisplacement:F2} m³");
        _output.WriteLine($"Error: {Math.Abs(result.Displacement - expectedDisplacement):F2} m³ ({Math.Abs(result.Displacement - expectedDisplacement) / expectedDisplacement * 100:F2}%)");

        result.Displacement.Should().BeApproximately(expectedDisplacement, 10,
            "Rectangular barge displacement should match analytical solution (±10 m³ = ±0.1%)");
    }

    [Fact]
    public void RectangularBarge_KB_MatchesAnalyticalSolution()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateRectangularBarge();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: KB = Draft / 2 = 5m / 2 = 2.5m (for rectangular barge)
        var expectedKB = 2.5;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed KB: {result.KB:F3} m");
        _output.WriteLine($"Expected KB: {expectedKB:F3} m");
        _output.WriteLine($"Error: {Math.Abs(result.KB - expectedKB):F3} m");

        result.KB.Should().BeApproximately(expectedKB, 0.05,
            "KB for rectangular barge should be draft/2 (±5cm)");
    }

    [Fact]
    public void RectangularBarge_WaterplaneArea_MatchesAnalyticalSolution()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateRectangularBarge();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: Awp = Length × Breadth = 100m × 20m = 2,000 m²
        var expectedAwp = 2000.0;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed Awp: {result.WaterplaneArea:F2} m²");
        _output.WriteLine($"Expected Awp: {expectedAwp:F2} m²");

        result.WaterplaneArea.Should().BeApproximately(expectedAwp, 20,
            "Waterplane area should match Length × Breadth (±20 m² = ±1%)");
    }

    [Fact]
    public void RectangularBarge_FormCoefficients_AreUnity()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateRectangularBarge();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: All form coefficients = 1.0 for rectangular barge
        var expectedCb = 1.0;
        var expectedCp = 1.0;
        var expectedCm = 1.0;
        var expectedCwp = 1.0;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Cb: {result.Cb:F4} (expected: {expectedCb:F4})");
        _output.WriteLine($"Cp: {result.Cp:F4} (expected: {expectedCp:F4})");
        _output.WriteLine($"Cm: {result.Cm:F4} (expected: {expectedCm:F4})");
        _output.WriteLine($"Cwp: {result.Cwp:F4} (expected: {expectedCwp:F4})");

        result.Cb.Should().BeApproximately(expectedCb, 0.01, "Cb should be 1.0 for rectangular barge");
        result.Cp.Should().BeApproximately(expectedCp, 0.01, "Cp should be 1.0 for rectangular barge");
        result.Cm.Should().BeApproximately(expectedCm, 0.01, "Cm should be 1.0 for rectangular barge");
        result.Cwp.Should().BeApproximately(expectedCwp, 0.01, "Cwp should be 1.0 for rectangular barge");
    }

    [Fact]
    public void RectangularBarge_BMt_MatchesAnalyticalSolution()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateRectangularBarge();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: BMt = Iwp / ∇
        // Iwp = (B³ × L) / 12 = (20³ × 100) / 12 = 66,666.67 m⁴
        // ∇ = 10,000 m³
        // BMt = 66,666.67 / 10,000 = 6.67 m
        var expectedBMt = 6.67;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed BMt: {result.BMt:F3} m");
        _output.WriteLine($"Expected BMt: {expectedBMt:F3} m");

        result.BMt.Should().BeApproximately(expectedBMt, 0.1,
            "BMt should match Iwp/∇ (±10cm = ±1.5%)");
    }

    [Fact]
    public void RectangularBarge_LCB_IsAtAmidships()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateRectangularBarge();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: LCB = 50m (amidships) for symmetric barge
        // Or LCB = 0m if measured from amidships
        var expectedLCBFromAft = 50.0;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed LCB: {result.LCB:F3} m from aft");
        _output.WriteLine($"Expected LCB: {expectedLCBFromAft:F3} m from aft");

        // LCB should be very close to amidships
        result.LCB.Should().BeApproximately(expectedLCBFromAft, 1.0,
            "LCB should be at amidships for symmetric barge (±1m = ±2%)");
    }

    #endregion

    #region Wigley Hull (ITTC Benchmark)

    [Fact(Skip = "Requires Wigley hull offsets CSV data - see E2E_TEST_DATA_REQUIREMENTS.md")]
    public void WigleyHull_Cb_MatchesPublishedValue()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateWigleyHull();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: Cb = 0.444 (published ITTC value)
        var expectedCb = 0.444;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed Cb: {result.Cb:F4}");
        _output.WriteLine($"Expected Cb: {expectedCb:F4}");
        _output.WriteLine($"Error: {Math.Abs(result.Cb - expectedCb):F4} ({Math.Abs(result.Cb - expectedCb) / expectedCb * 100:F2}%)");

        result.Cb.Should().BeApproximately(expectedCb, 0.005,
            "Wigley hull Cb should match published ITTC value (±0.5%)");
    }

    [Fact(Skip = "Requires Wigley hull offsets CSV data - see E2E_TEST_DATA_REQUIREMENTS.md")]
    public void WigleyHull_Cp_MatchesTheoreticalValue()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateWigleyHull();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: Cp = 0.666 (parabolic waterlines)
        var expectedCp = 0.666;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed Cp: {result.Cp:F4}");
        _output.WriteLine($"Expected Cp: {expectedCp:F4}");

        result.Cp.Should().BeApproximately(expectedCp, 0.01,
            "Wigley hull Cp should match theoretical parabolic value (±1%)");
    }

    [Fact(Skip = "Requires Wigley hull offsets CSV data")]
    public void WigleyHull_LCB_IsCentered()
    {
        // Arrange
        var vessel = TestDataGenerator.CreateWigleyHull();
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: LCB = 0.0m from amidships (symmetric hull)
        var expectedLCBFromAmidships = 0.0;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // LCB might be reported from aft, so convert to from amidships
        var lcbFromAmidships = result.LCB - (vessel.LengthOverall / 2);

        // Assert
        _output.WriteLine($"Computed LCB from amidships: {lcbFromAmidships:F4} m");

        lcbFromAmidships.Should().BeApproximately(expectedLCBFromAmidships, 0.01,
            "Wigley hull LCB should be at amidships (symmetric hull)");
    }

    #endregion

    #region Series 60 (Cb = 0.60)

    [Fact(Skip = "Requires Series 60 offsets CSV data - see E2E_TEST_DATA_REQUIREMENTS.md")]
    public void Series60_Cb_MatchesSeriesValue()
    {
        // Arrange
        var vessel = LoadBenchmarkVessel("Series60_Cb060");
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: Cb = 0.600 (by definition)
        var expectedCb = 0.600;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed Cb: {result.Cb:F4}");
        _output.WriteLine($"Expected Cb: {expectedCb:F4}");

        result.Cb.Should().BeApproximately(expectedCb, 0.005,
            "Series 60 Cb=0.60 hull should match series value (±0.5%)");
    }

    [Fact(Skip = "Requires Series 60 hydrostatic data")]
    public void Series60_HydrostaticProperties_MatchPublishedData()
    {
        // This test will compare complete hydrostatic curves against DTMB published data
        // once we have the offsets and reference curves
        Assert.True(true, "Placeholder - requires Series 60 reference data");
    }

    #endregion

    #region KCS Container Ship

    [Fact(Skip = "Requires KCS offsets CSV data - see E2E_TEST_DATA_REQUIREMENTS.md")]
    public void KCS_Cb_MatchesPublishedValue()
    {
        // Arrange
        var vessel = LoadBenchmarkVessel("KCS");
        var loadcase = TestDataGenerator.CreateLoadcase(vessel.Id);

        // Expected: Cb = 0.651 (published value)
        var expectedCb = 0.651;

        // Act
        var result = _hydroCalculator.ComputeHydrostatics(vessel, loadcase);

        // Assert
        _output.WriteLine($"Computed Cb: {result.Cb:F4}");
        _output.WriteLine($"Expected Cb: {expectedCb:F4}");

        result.Cb.Should().BeApproximately(expectedCb, 0.005,
            "KCS Cb should match published value (±0.5%)");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Load benchmark vessel from test data directory
    /// </summary>
    private Vessel LoadBenchmarkVessel(string vesselName)
    {
        // TODO: Implement loading from test-data/vessels/benchmarks/{vesselName}/
        // This will load offsets CSV and create Vessel entity
        throw new NotImplementedException($"Benchmark vessel '{vesselName}' data not yet available. See E2E_TEST_DATA_REQUIREMENTS.md");
    }

    /// <summary>
    /// Load expected results from JSON file
    /// </summary>
    private Dictionary<string, double> LoadExpectedResults(string vesselName)
    {
        // TODO: Load from test-data/vessels/benchmarks/{vesselName}/expected-results.json
        throw new NotImplementedException($"Expected results for '{vesselName}' not yet available");
    }

    /// <summary>
    /// Compare computed results against expected values with tolerance
    /// </summary>
    private void AssertHydrostaticResults(
        HydroResult computed,
        Dictionary<string, double> expected,
        double tolerance = 0.02) // 2% default tolerance
    {
        foreach (var (property, expectedValue) in expected)
        {
            var computedValue = GetPropertyValue(computed, property);
            var error = Math.Abs(computedValue - expectedValue);
            var percentError = error / expectedValue * 100;

            _output.WriteLine($"{property}: {computedValue:F4} (expected: {expectedValue:F4}, error: {percentError:F2}%)");

            computedValue.Should().BeApproximately(expectedValue, expectedValue * tolerance,
                $"{property} should be within {tolerance * 100}% of expected value");
        }
    }

    private double GetPropertyValue(HydroResult result, string propertyName)
    {
        var property = typeof(HydroResult).GetProperty(propertyName);
        return property != null ? Convert.ToDouble(property.GetValue(result)) : 0.0;
    }

    #endregion
}
