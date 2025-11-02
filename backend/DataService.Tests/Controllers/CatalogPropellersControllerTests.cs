using DataService.Controllers;
using DataService.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Controllers;

public class CatalogPropellersControllerTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly CatalogPropellersController _controller;
    private readonly Mock<ILogger<CatalogPropellersController>> _loggerMock;
    private CatalogPropellerSeries _testSeries3Blades = null!;
    private CatalogPropellerSeries _testSeries4Blades = null!;

    public CatalogPropellersControllerTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _loggerMock = new Mock<ILogger<CatalogPropellersController>>();

        _controller = new CatalogPropellersController(
            _context,
            _loggerMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create a 3-blade propeller series
        _testSeries3Blades = new CatalogPropellerSeries
        {
            Id = Guid.NewGuid(),
            Name = "Wageningen B-Series (Z=3, AE/A0=0.5, P/D=0.8)",
            BladeCount = 3,
            ExpandedAreaRatio = 0.5m,
            PitchDiameterRatio = 0.8m,
            SourceUrl = "https://test.com/b-series",
            License = "Public Domain",
            IsDemo = false,
            CreatedAt = DateTime.UtcNow
        };

        // Add some open-water points
        _testSeries3Blades.OpenWaterPoints = new List<CatalogPropellerPoint>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SeriesId = _testSeries3Blades.Id,
                J = 0.1m,
                Kt = 0.5m,
                Kq = 0.08m,
                Eta0 = 0.3m,
                ReynoldsNumber = 1e6m
            },
            new()
            {
                Id = Guid.NewGuid(),
                SeriesId = _testSeries3Blades.Id,
                J = 0.5m,
                Kt = 0.4m,
                Kq = 0.06m,
                Eta0 = 0.5m,
                ReynoldsNumber = 1e6m
            },
            new()
            {
                Id = Guid.NewGuid(),
                SeriesId = _testSeries3Blades.Id,
                J = 0.9m,
                Kt = 0.2m,
                Kq = 0.03m,
                Eta0 = 0.6m,
                ReynoldsNumber = 1e6m
            }
        };

        _context.CatalogPropellerSeries.Add(_testSeries3Blades);

        // Create a 4-blade propeller series (demo data)
        _testSeries4Blades = new CatalogPropellerSeries
        {
            Id = Guid.NewGuid(),
            Name = "Demo B-Series (Z=4, AE/A0=0.6, P/D=1.0)",
            BladeCount = 4,
            ExpandedAreaRatio = 0.6m,
            PitchDiameterRatio = 1.0m,
            SourceUrl = null,
            License = "Demo",
            IsDemo = true,
            CreatedAt = DateTime.UtcNow
        };

        _testSeries4Blades.OpenWaterPoints = new List<CatalogPropellerPoint>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SeriesId = _testSeries4Blades.Id,
                J = 0.2m,
                Kt = 0.45m,
                Kq = 0.07m,
                Eta0 = 0.4m,
                ReynoldsNumber = 5e5m
            }
        };

        _context.CatalogPropellerSeries.Add(_testSeries4Blades);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ListSeries_NoFilter_ReturnsAllSeries()
    {
        // Act
        var result = await _controller.ListSeries();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var series = okResult.Value.Should().BeAssignableTo<List<CatalogPropellerSeriesDto>>().Subject;

        series.Should().HaveCount(2);
        series.Should().Contain(s => s.BladeCount == 3);
        series.Should().Contain(s => s.BladeCount == 4);
    }

    [Fact]
    public async Task ListSeries_FilterByBladeCount_ReturnsFilteredSeries()
    {
        // Act
        var result = await _controller.ListSeries(bladeCount: 3);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var series = okResult.Value.Should().BeAssignableTo<List<CatalogPropellerSeriesDto>>().Subject;

        series.Should().HaveCount(1);
        series.First().BladeCount.Should().Be(3);
        series.First().Name.Should().Contain("Z=3");
    }

    [Fact]
    public async Task ListSeries_ReturnsCorrectPointsCount()
    {
        // Act
        var result = await _controller.ListSeries();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var series = okResult.Value.Should().BeAssignableTo<List<CatalogPropellerSeriesDto>>().Subject;

        var series3Blades = series.First(s => s.BladeCount == 3);
        var series4Blades = series.First(s => s.BladeCount == 4);

        series3Blades.PointsCount.Should().Be(3);
        series4Blades.PointsCount.Should().Be(1);
    }

    [Fact]
    public async Task ListSeries_OrderedByBladeCountAndAERatio()
    {
        // Act
        var result = await _controller.ListSeries();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var series = okResult.Value.Should().BeAssignableTo<List<CatalogPropellerSeriesDto>>().Subject;

        // Should be ordered by blade count first, then by expanded area ratio
        series.Should().BeInAscendingOrder(s => s.BladeCount);
        series.First().BladeCount.Should().Be(3);
        series.Last().BladeCount.Should().Be(4);
    }

    [Fact]
    public async Task GetSeries_ValidId_ReturnsSeriesWithPoints()
    {
        // Act
        var result = await _controller.GetSeries(_testSeries3Blades.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var series = okResult.Value.Should().BeAssignableTo<CatalogPropellerSeriesDetailsDto>().Subject;

        series.Id.Should().Be(_testSeries3Blades.Id);
        series.BladeCount.Should().Be(3);
        series.ExpandedAreaRatio.Should().Be(0.5m);
        series.PitchDiameterRatio.Should().Be(0.8m);
        series.IsDemo.Should().BeFalse();
        series.PointsCount.Should().Be(3);
        series.Points.Should().HaveCount(3);

        // Verify points are included
        series.Points.Should().Contain(p => p.J == 0.1m);
        series.Points.Should().Contain(p => p.J == 0.5m);
        series.Points.Should().Contain(p => p.J == 0.9m);
    }

    [Fact]
    public async Task GetSeries_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSeries(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPoints_ValidId_ReturnsOrderedPoints()
    {
        // Act
        var result = await _controller.GetPoints(_testSeries3Blades.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var points = okResult.Value.Should().BeAssignableTo<List<CatalogPropellerPointDto>>().Subject;

        points.Should().HaveCount(3);
        points.Should().BeInAscendingOrder(p => p.J);

        // Verify all points have required fields
        points.Should().AllSatisfy(p =>
        {
            p.SeriesId.Should().Be(_testSeries3Blades.Id);
            p.Kt.Should().BeGreaterThan(0);
            p.Kq.Should().BeGreaterThan(0);
            p.Eta0.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task GetPoints_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetPoints(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSeries_DemoSeries_HasCorrectFlags()
    {
        // Act
        var result = await _controller.GetSeries(_testSeries4Blades.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var series = okResult.Value.Should().BeAssignableTo<CatalogPropellerSeriesDetailsDto>().Subject;

        series.IsDemo.Should().BeTrue();
        series.License.Should().Be("Demo");
        series.SourceUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetPoints_VerifyDataIntegrity()
    {
        // Act
        var result = await _controller.GetPoints(_testSeries3Blades.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var points = okResult.Value.Should().BeAssignableTo<List<CatalogPropellerPointDto>>().Subject;

        // Verify specific data point values
        var lowJPoint = points.First(p => p.J == 0.1m);
        lowJPoint.Kt.Should().Be(0.5m);
        lowJPoint.Kq.Should().Be(0.08m);
        lowJPoint.Eta0.Should().Be(0.3m);

        var highJPoint = points.First(p => p.J == 0.9m);
        highJPoint.Kt.Should().Be(0.2m);
        highJPoint.Kq.Should().Be(0.03m);
        highJPoint.Eta0.Should().Be(0.6m);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
