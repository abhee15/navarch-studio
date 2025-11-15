using Shared.HullGenerators.ParentHull;

namespace Shared.Tests.HullGenerators.ParentHull;

/// <summary>
/// Tests for parent hull loader
/// </summary>
public class ParentHullLoaderTests
{
    [Fact]
    public void HasParentHull_WithProductCarrierCb080_ReturnsTrue()
    {
        // Act
        var hasHull = ParentHullLoader.HasParentHull("product_carrier", 0.80m);

        // Assert
        hasHull.Should().BeTrue();
    }

    [Fact]
    public void HasParentHull_WithUnknownVesselType_ReturnsFalse()
    {
        // Act
        var hasHull = ParentHullLoader.HasParentHull("unknown_type", 0.80m);

        // Assert
        hasHull.Should().BeFalse();
    }

    [Fact]
    public void LoadParentHull_WithProductCarrierCb080_LoadsSuccessfully()
    {
        // Arrange
        var loader = new ParentHullLoader();

        // Act
        var parentHull = loader.LoadParentHull("product_carrier", 0.80m);

        // Assert
        parentHull.Should().NotBeNull();
        parentHull.VesselType.Should().Be("product_carrier");
        parentHull.Cb.Should().Be(0.80m);
        parentHull.Stations.Should().NotBeEmpty();
        parentHull.Waterlines.Should().NotBeEmpty();
        parentHull.Offsets.Should().NotBeEmpty();
        parentHull.Offsets.Count.Should().Be(parentHull.Stations.Count);
        parentHull.Offsets[0].Count.Should().Be(parentHull.Waterlines.Count);
    }

    [Fact]
    public void LoadParentHull_WithUnknownVesselType_ThrowsException()
    {
        // Arrange
        var loader = new ParentHullLoader();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            loader.LoadParentHull("unknown_type", 0.80m));
    }
}
