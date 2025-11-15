using Shared.HullGenerators;
using Shared.HullGenerators.Models;
using Shared.HullGenerators.ParentHull;

namespace Shared.Tests.HullGenerators.Integration;

/// <summary>
/// Integration tests for parent hull generator with real vessel data
/// These tests verify end-to-end functionality with actual parent hull data
/// </summary>
[Trait("Category", "Integration")]
public class ParentHullGeneratorIntegrationTests
{
    [Fact]
    public void Generate_ProductCarrierCb080_ProducesValidGeometry()
    {
        // Arrange - Product Carrier reference dimensions from BSRA
        var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
        var generator = new ParentHullHullGenerator(null, "product_carrier");
        decimal cb = 0.80m;
        decimal cp = 0.82m;
        decimal cm = 0.99m;
        decimal cwp = 0.87m;

        // Act
        var geometry = generator.Generate(dims, cb, cp, cm, cwp, numStations: 23, numWaterlines: 13);

        // Assert
        geometry.Should().NotBeNull();
        geometry.Stations.Should().HaveCount(23);
        geometry.Waterlines.Should().NotBeEmpty();
        geometry.Offsets.Should().HaveCount(23);
        geometry.Offsets[0].Should().HaveCount(geometry.Waterlines.Count);

        // Verify computed coefficients are reasonable
        geometry.ComputedCoefficients.Should().NotBeNull();
        var computed = geometry.ComputedCoefficients!;
        computed.Cb.Should().BeGreaterThan(0.7m).And.BeLessThan(0.9m);
        computed.Volume.Should().BeGreaterThan(0);

        // Verify offsets are non-negative
        foreach (var stationOffsets in geometry.Offsets)
        {
            stationOffsets.Should().OnlyContain(hb => hb >= 0);
        }
    }

    [Fact]
    public void Generate_WithFactory_SelectsCorrectGenerator()
    {
        // Arrange
        var factory = new HullGeneratorFactory();
        var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);

        // Act - Product Carrier with Cb=0.80 should use parent hull
        var generator = factory.GetGenerator("product_carrier", 0.80m);
        var geometry = generator.Generate(dims, 0.80m, 0.82m, 0.99m, 0.87m);

        // Assert
        generator.Should().BeOfType<ParentHullHullGenerator>();
        geometry.Should().NotBeNull();
        geometry.Stations.Should().HaveCount(23);
    }

    [Fact]
    public void Generate_WithFactory_UnknownVesselType_FallsBackToParametric()
    {
        // Arrange
        var factory = new HullGeneratorFactory();
        var dims = new HullDimensions(200m, 30m, 10m, 2.0m);

        // Act - Unknown vessel type should use parametric
        var generator = factory.GetGenerator("unknown_type", 0.75m);
        var geometry = generator.Generate(dims, 0.75m, 0.77m, 0.98m, 0.85m);

        // Assert
        generator.Should().BeOfType<FormCoefficientHullGenerator>();
        geometry.Should().NotBeNull();
        geometry.Stations.Should().HaveCount(23);
    }

    [Fact]
    public void Generate_ProductCarrier_ValidatesAgainstTargetCoefficients()
    {
        // Arrange
        var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
        var generator = new ParentHullHullGenerator(null, "product_carrier");
        decimal cb = 0.80m;
        decimal cp = 0.82m;
        decimal cm = 0.99m;
        decimal cwp = 0.87m;

        // Act
        var geometry = generator.Generate(dims, cb, cp, cm, cwp);

        // Assert - Check that computed Cb is close to target (within 5% tolerance for integration test)
        var computed = geometry.ComputedCoefficients!;
        var cbError = Math.Abs(computed.Cb - cb) / cb;
        cbError.Should().BeLessThan(0.05m, "Cb should match within 5% for integration test");
    }

    [Fact]
    public void Generate_DifferentDimensions_ScalesCorrectly()
    {
        // Arrange - Scale up the Product Carrier
        var originalDims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
        var scaledDims = new HullDimensions(200m, 30m, 13.5m, 2.0m); // ~8% larger
        var generator = new ParentHullHullGenerator(null, "product_carrier");
        decimal cb = 0.80m;

        // Act
        var originalGeometry = generator.Generate(originalDims, cb, 0.82m, 0.99m, 0.87m);
        var scaledGeometry = generator.Generate(scaledDims, cb, 0.82m, 0.99m, 0.87m);

        // Assert - Scaled geometry should have larger volume
        scaledGeometry.ComputedCoefficients!.Volume.Should().BeGreaterThan(
            originalGeometry.ComputedCoefficients!.Volume);

        // Volume should scale approximately with L*B*T ratio
        var volumeRatio = scaledGeometry.ComputedCoefficients.Volume / originalGeometry.ComputedCoefficients.Volume;
        var dimensionRatio = (200m * 30m * 13.5m) / (185m * 28m * 12.87m);
        volumeRatio.Should().BeApproximately(dimensionRatio, 0.1m); // Within 10% due to scaling effects
    }
}
