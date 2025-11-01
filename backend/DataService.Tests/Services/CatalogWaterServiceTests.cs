using DataService.Data;
using DataService.Services.Catalog;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Services;

public class CatalogWaterServiceTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly CatalogWaterService _service;
    private readonly Mock<ILogger<CatalogWaterService>> _loggerMock;

    public CatalogWaterServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _loggerMock = new Mock<ILogger<CatalogWaterService>>();
        _service = new CatalogWaterService(_context, _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var waterProps = new[]
        {
            // Freshwater anchor points
            new CatalogWaterProperty
            {
                Medium = "Fresh",
                Temperature_C = 0,
                Salinity_PSU = 0,
                Density_kgm3 = 999.8425m,
                KinematicViscosity_m2s = 0.000001792m,
                SourceRef = "ITTC Test"
            },
            new CatalogWaterProperty
            {
                Medium = "Fresh",
                Temperature_C = 15,
                Salinity_PSU = 0,
                Density_kgm3 = 999.1026m,
                KinematicViscosity_m2s = 0.000001139m,
                SourceRef = "ITTC Test"
            },
            new CatalogWaterProperty
            {
                Medium = "Fresh",
                Temperature_C = 30,
                Salinity_PSU = 0,
                Density_kgm3 = 995.6502m,
                KinematicViscosity_m2s = 0.000000801m,
                SourceRef = "ITTC Test"
            },
            // Seawater anchor points
            new CatalogWaterProperty
            {
                Medium = "Sea",
                Temperature_C = 0,
                Salinity_PSU = 35,
                Density_kgm3 = 1028.106m,
                KinematicViscosity_m2s = 0.000001829m,
                SourceRef = "ITTC Test"
            },
            new CatalogWaterProperty
            {
                Medium = "Sea",
                Temperature_C = 15,
                Salinity_PSU = 35,
                Density_kgm3 = 1025.970m,
                KinematicViscosity_m2s = 0.000001160m,
                SourceRef = "ITTC Test"
            },
            new CatalogWaterProperty
            {
                Medium = "Sea",
                Temperature_C = 30,
                Salinity_PSU = 35,
                Density_kgm3 = 1021.830m,
                KinematicViscosity_m2s = 0.000000783m,
                SourceRef = "ITTC Test"
            }
        };

        _context.CatalogWaterProperties.AddRange(waterProps);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetWaterPropertiesAsync_ExactMatch_ReturnsAnchorPoint()
    {
        // Arrange
        var temperatureC = 15m;
        var salinityPSU = 0m;

        // Act
        var result = await _service.GetWaterPropertiesAsync(temperatureC, salinityPSU);

        // Assert
        result.Should().NotBeNull();
        result.Medium.Should().Be("Fresh");
        result.Temperature_C.Should().Be(15m);
        result.Density.Should().Be(999.1026m);
        result.KinematicViscosity_m2s.Should().Be(0.000001139m);
        result.IsInterpolated.Should().BeFalse();
    }

    [Fact]
    public async Task GetWaterPropertiesAsync_Interpolation_ReturnsInterpolatedValues()
    {
        // Arrange
        var temperatureC = 10m; // Between 0 and 15
        var salinityPSU = 0m;

        // Act
        var result = await _service.GetWaterPropertiesAsync(temperatureC, salinityPSU);

        // Assert
        result.Should().NotBeNull();
        result.Medium.Should().Be("Fresh");
        result.Temperature_C.Should().Be(10m);
        result.IsInterpolated.Should().BeTrue();
        
        // Density should be between 999.8425 (0°C) and 999.1026 (15°C)
        result.Density.Should().BeGreaterThan(999.1026m);
        result.Density.Should().BeLessThan(999.8425m);
        
        // Viscosity should be between values at 0°C and 15°C
        result.KinematicViscosity_m2s.Should().BeGreaterThan(0.000001139m);
        result.KinematicViscosity_m2s.Should().BeLessThan(0.000001792m);
    }

    [Fact]
    public async Task GetWaterPropertiesAsync_Seawater_ReturnsSeawaterProperties()
    {
        // Arrange
        var temperatureC = 15m;
        var salinityPSU = 35m;

        // Act
        var result = await _service.GetWaterPropertiesAsync(temperatureC, salinityPSU);

        // Assert
        result.Should().NotBeNull();
        result.Medium.Should().Be("Sea");
        result.Temperature_C.Should().Be(15m);
        result.Salinity_PSU.Should().Be(35m);
        result.Density.Should().Be(1025.970m);
        result.KinematicViscosity_m2s.Should().Be(0.000001160m);
        result.IsInterpolated.Should().BeFalse();
    }

    [Fact]
    public async Task GetWaterPropertiesAsync_OutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var temperatureC = 35m; // Above max 30°C
        var salinityPSU = 0m;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetWaterPropertiesAsync(temperatureC, salinityPSU));
    }

    [Fact]
    public async Task GetAnchorPointsAsync_Fresh_ReturnsOnlyFreshwaterPoints()
    {
        // Act
        var result = await _service.GetAnchorPointsAsync("Fresh");

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.Medium.Should().Be("Fresh"));
        result.Should().BeInAscendingOrder(p => p.Temperature_C);
    }

    [Fact]
    public async Task GetAnchorPointsAsync_Sea_ReturnsOnlySeawaterPoints()
    {
        // Act
        var result = await _service.GetAnchorPointsAsync("Sea");

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.Medium.Should().Be("Sea"));
        result.Should().BeInAscendingOrder(p => p.Temperature_C);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllWaterProperties()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(6); // 3 fresh + 3 sea
        result.Should().Contain(p => p.Medium == "Fresh");
        result.Should().Contain(p => p.Medium == "Sea");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

