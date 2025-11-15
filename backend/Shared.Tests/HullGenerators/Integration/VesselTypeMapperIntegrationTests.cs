using Shared.HullGenerators;

namespace Shared.Tests.HullGenerators.Integration;

/// <summary>
/// Integration tests for vessel type mapping
/// </summary>
[Trait("Category", "Integration")]
public class VesselTypeMapperIntegrationTests
{
    [Theory]
    [InlineData("product_carrier", "product_carrier")]
    [InlineData("Product Carrier", "product_carrier")]
    [InlineData("PRODUCT_CARRIER", "product_carrier")]
    [InlineData("tanker", "tanker")]
    [InlineData("Oil Tanker", "tanker")]
    [InlineData("container", "container")]
    [InlineData("Container Ship", "container")]
    [InlineData("bulk_carrier", "bulk_carrier")]
    [InlineData("Bulk Carrier", "bulk_carrier")]
    [InlineData("general_cargo", "general_cargo")]
    [InlineData("General Cargo", "general_cargo")]
    [InlineData("Multi-purpose", "general_cargo")]
    public void MapToRegistryType_VariousInputs_MapsCorrectly(string input, string expected)
    {
        // Act
        var result = VesselTypeMapper.MapToRegistryType(input, null);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void MapToRegistryType_UnknownType_ReturnsNull()
    {
        // Act
        var result = VesselTypeMapper.MapToRegistryType("unknown_type", null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void MapToRegistryType_PrefersShipdType_OverVesselType()
    {
        // Act
        var result = VesselTypeMapper.MapToRegistryType("tanker", "container");

        // Assert
        result.Should().Be("tanker");
    }
}
