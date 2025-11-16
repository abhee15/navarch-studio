using Shared.HullGenerators;
using Shared.HullGenerators.ParentHull;

namespace Shared.Tests.HullGenerators;

/// <summary>
/// Tests for hull generator factory
/// </summary>
public class HullGeneratorFactoryTests
{
    [Fact]
    public void GetGenerator_WithProductCarrierCb080_ReturnsParentHullGenerator()
    {
        // Arrange
        var factory = new HullGeneratorFactory();

        // Act
        var generator = factory.GetGenerator("product_carrier", 0.80m);

        // Assert
        generator.Should().NotBeNull();
        // Factory now returns vessel-type-specific generator (ProductCarrierHullGenerator)
        // which is a parent hull generator, so we check for the base type
        generator.Should().BeAssignableTo<ParentHullHullGenerator>();
    }

    [Fact]
    public void GetGenerator_WithUnknownVesselType_ReturnsParametricGenerator()
    {
        // Arrange
        var factory = new HullGeneratorFactory();

        // Act
        var generator = factory.GetGenerator("unknown_type", 0.80m);

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeOfType<FormCoefficientHullGenerator>();
    }

    [Fact]
    public void GetGenerator_WithProductCarrierButUnavailableCb_ReturnsParametricGenerator()
    {
        // Arrange
        var factory = new HullGeneratorFactory();

        // Act
        var generator = factory.GetGenerator("product_carrier", 0.50m); // Cb not in registry

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeOfType<FormCoefficientHullGenerator>();
    }
}
