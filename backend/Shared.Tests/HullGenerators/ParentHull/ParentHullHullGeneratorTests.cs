using Shared.HullGenerators;
using Shared.HullGenerators.Models;
using Shared.HullGenerators.ParentHull;

namespace Shared.Tests.HullGenerators.ParentHull;

/// <summary>
/// Tests for parent hull generator
/// </summary>
public class ParentHullHullGeneratorTests
{
    [Fact]
    public void Generate_WithProductCarrierCb080_GeneratesValidGeometry()
    {
        // Arrange - Product Carrier reference dimensions
        var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
        var generator = new ParentHullHullGenerator();
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
        geometry.ComputedCoefficients.Should().NotBeNull();
    }

    [Fact]
    public void Generate_WithNon23Stations_ThrowsException()
    {
        // Arrange
        var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
        var generator = new ParentHullHullGenerator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            generator.Generate(dims, 0.80m, 0.82m, 0.99m, 0.87m, numStations: 20));
    }

    [Fact]
    public void Generate_WithUnknownVesselType_ThrowsException()
    {
        // Arrange
        var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
        var generator = new ParentHullHullGenerator();

        // Act & Assert
        // Should throw when parent hull not available
        Assert.Throws<InvalidOperationException>(() =>
            generator.Generate(dims, 0.50m, 0.52m, 0.90m, 0.75m, numStations: 23));
    }
}
