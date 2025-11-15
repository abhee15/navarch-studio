using Shared.HullGenerators;
using Shared.HullGenerators.ParentHull;

namespace Shared.Tests.HullGenerators;

/// <summary>
/// Tests for hull generator factory
/// </summary>
public class HullGeneratorFactoryTests
{
    private static bool HasDataFiles()
    {
        try
        {
            ParentHullLoader.HasParentHull("product_carrier", 0.80m);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    [Fact(Skip = "Requires parent hull CSV data files - skipped in CI/CD if files not available")]
    public void GetGenerator_WithProductCarrierCb080_ReturnsParentHullGenerator()
    {
        if (!HasDataFiles())
            return; // Skip if data files not available

        // Arrange
        var factory = new HullGeneratorFactory();

        // Act
        var generator = factory.GetGenerator("product_carrier", 0.80m);

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeOfType<ParentHullHullGenerator>();
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

    [Fact(Skip = "Requires parent hull CSV data files - skipped in CI/CD if files not available")]
    public void GetGenerator_WithProductCarrierButUnavailableCb_ReturnsParametricGenerator()
    {
        if (!HasDataFiles())
            return; // Skip if data files not available

        // Arrange
        var factory = new HullGeneratorFactory();

        // Act
        var generator = factory.GetGenerator("product_carrier", 0.50m); // Cb not in registry

        // Assert
        generator.Should().NotBeNull();
        generator.Should().BeOfType<FormCoefficientHullGenerator>();
    }
}
