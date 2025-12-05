using FluentAssertions;
using HullSizingService.Services.ShipD;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HullSizingService.Tests.Services;

/// <summary>
/// Unit tests for VesselTypeHullMappingService.
/// Verifies that vessel types map to correct hull families and parameters.
/// </summary>
public class VesselTypeHullMappingServiceTests
{
    private readonly Mock<ILogger<VesselTypeHullMappingService>> _loggerMock;
    private readonly IVesselTypeHullMappingService _service;

    public VesselTypeHullMappingServiceTests()
    {
        _loggerMock = new Mock<ILogger<VesselTypeHullMappingService>>();
        _service = new VesselTypeHullMappingService(_loggerMock.Object);
    }

    [Fact]
    public void GetDefaultsForVesselType_ContainerShip_ReturnsSoftChineConvex()
    {
        // Arrange
        var category = "commercial";
        var type = "container";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().NotBeNull();
        defaults!.BowFamily.Should().Be("bulbous_bow");
        defaults.MidshipFamily.Should().Be("barge_type");
        defaults.SternFamily.Should().Be("transom_stern");
        defaults.ChineType.Should().Be("soft");
        defaults.CurvatureType.Should().Be("convex");
        defaults.DeadriseAngleDeg.Should().Be(10m);
        defaults.FlareAngleDeg.Should().Be(5m);
    }

    [Fact]
    public void GetDefaultsForVesselType_SailingYacht_ReturnsSoftChineConvex()
    {
        // Arrange
        var category = "recreational";
        var type = "yacht";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().NotBeNull();
        defaults!.BowFamily.Should().Be("straight_raked");
        defaults.MidshipFamily.Should().Be("fine_midship");
        defaults.SternFamily.Should().Be("cruiser_stern");
        defaults.ChineType.Should().Be("soft");
        defaults.CurvatureType.Should().Be("convex");
        defaults.DeadriseAngleDeg.Should().Be(15m);
        defaults.FlareAngleDeg.Should().Be(8m);
    }

    [Fact]
    public void GetDefaultsForVesselType_HighSpeedCraft_ReturnsHardChine()
    {
        // Arrange
        var category = "specialized";
        var type = "high_speed_craft";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().NotBeNull();
        defaults!.BowFamily.Should().Be("straight_raked");
        defaults.MidshipFamily.Should().Be("deep_v");
        defaults.SternFamily.Should().Be("transom_stern");
        defaults.ChineType.Should().Be("hard");
        defaults.CurvatureType.Should().BeNull(); // Neutral for planing hull
        defaults.DeadriseAngleDeg.Should().Be(10m);
    }

    [Fact]
    public void GetDefaultsForVesselType_GeneralMilitary_ReturnsTumblehome()
    {
        // Arrange
        var category = "government";
        var type = "general_military";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().NotBeNull();
        defaults!.BowFamily.Should().Be("straight_raked");
        defaults.MidshipFamily.Should().Be("fine_midship");
        defaults.SternFamily.Should().Be("transom_stern");
        defaults.ChineType.Should().Be("soft");
        defaults.CurvatureType.Should().Be("concave");
        defaults.FlareAngleDeg.Should().Be(-15m); // Negative = tumblehome
        defaults.TumblehomeEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetDefaultsForVesselType_Cutters_ReturnsHardChine()
    {
        // Arrange
        var category = "government";
        var type = "cutters";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().NotBeNull();
        defaults!.ChineType.Should().Be("hard");
        defaults.CurvatureType.Should().BeNull(); // Neutral
        defaults.FlareAngleDeg.Should().Be(8m); // Positive = flare
    }

    [Fact]
    public void GetDefaultsForVesselType_UnknownType_ReturnsNull()
    {
        // Arrange
        var category = "unknown";
        var type = "unknown_type";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().BeNull();
    }

    [Fact]
    public void GetDefaultsForVesselType_EmptyCategory_ReturnsNull()
    {
        // Arrange
        var category = "";
        var type = "container";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().BeNull();
    }

    [Fact]
    public void GetDefaultsForVesselType_EmptyType_ReturnsNull()
    {
        // Arrange
        var category = "commercial";
        var type = "";

        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().BeNull();
    }

    [Theory]
    [InlineData("commercial", "general_cargo")]
    [InlineData("commercial", "bulk_carrier")]
    [InlineData("commercial", "tanker")]
    [InlineData("commercial", "lng_carrier")]
    [InlineData("commercial", "cruise_vessel")]
    [InlineData("commercial", "passenger_vessel")]
    public void GetDefaultsForVesselType_CommercialVessels_ReturnsSoftChineConvex(string category, string type)
    {
        // Act
        var defaults = _service.GetDefaultsForVesselType(category, type);

        // Assert
        defaults.Should().NotBeNull();
        defaults!.ChineType.Should().Be("soft");
        defaults.CurvatureType.Should().Be("convex");
        defaults.BowFamily.Should().Be("bulbous_bow");
        defaults.SternFamily.Should().Be("transom_stern");
    }
}

