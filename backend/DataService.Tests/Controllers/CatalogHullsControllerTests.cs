using DataService.Controllers;
using DataService.Data;
using DataService.Services.Hydrostatics;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Controllers;

public class CatalogHullsControllerTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly CatalogHullsController _controller;
    private readonly Mock<ILogger<CatalogHullsController>> _loggerMock;
    private readonly Mock<IVesselService> _vesselServiceMock;
    private readonly Mock<IGeometryService> _geometryServiceMock;
    private BenchmarkCase _testHull = null!;

    public CatalogHullsControllerTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _loggerMock = new Mock<ILogger<CatalogHullsController>>();
        _vesselServiceMock = new Mock<IVesselService>();
        _geometryServiceMock = new Mock<IGeometryService>();

        _controller = new CatalogHullsController(
            _context,
            _loggerMock.Object,
            _vesselServiceMock.Object,
            _geometryServiceMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        _testHull = new BenchmarkCase
        {
            Id = Guid.NewGuid(),
            Slug = "test-hull",
            Title = "Test Hull",
            Description = "A test hull for unit tests",
            HullType = "Template",
            Lpp_m = 100m,
            B_m = 20m,
            T_m = 10m,
            Cb = 0.7m,
            Cp = 0.75m,
            LCB_pctLpp = 2m,
            LCF_pctLpp = -1.5m,
            GeometryMissing = false,
            CanonicalRefs = "Test Reference",
            CreatedAt = DateTime.UtcNow
        };

        var geometry = new BenchmarkGeometry
        {
            Id = Guid.NewGuid(),
            CaseId = _testHull.Id,
            StationsJson = "[{\"index\":0,\"xPosition\":0}]",
            WaterlinesJson = "[{\"index\":0,\"zPosition\":0}]",
            OffsetsJson = "[{\"stationIndex\":0,\"waterlineIndex\":0,\"halfBreadth\":10}]",
            Type = "Offsets",
            SourceUrl = "https://test.com/geometry"
        };

        _testHull.Geometries = new List<BenchmarkGeometry> { geometry };

        _context.BenchmarkCases.Add(_testHull);

        // Add another hull without geometry
        var hullWithoutGeometry = new BenchmarkCase
        {
            Id = Guid.NewGuid(),
            Slug = "hull-no-geometry",
            Title = "Hull Without Geometry",
            Description = "Test hull without geometry",
            HullType = "Container",
            Lpp_m = 200m,
            B_m = 30m,
            T_m = 15m,
            Cb = 0.65m,
            GeometryMissing = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BenchmarkCases.Add(hullWithoutGeometry);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ListHulls_NoFilter_ReturnsAllHulls()
    {
        // Act
        var result = await _controller.ListHulls();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var hulls = okResult.Value.Should().BeAssignableTo<List<CatalogHullListItemDto>>().Subject;
        hulls.Should().HaveCount(2);
        hulls.Should().Contain(h => h.Slug == "test-hull");
        hulls.Should().Contain(h => h.Slug == "hull-no-geometry");
    }

    [Fact]
    public async Task ListHulls_FilterByType_ReturnsFilteredHulls()
    {
        // Act
        var result = await _controller.ListHulls(hullType: "Template");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var hulls = okResult.Value.Should().BeAssignableTo<List<CatalogHullListItemDto>>().Subject;
        hulls.Should().HaveCount(1);
        hulls.First().Slug.Should().Be("test-hull");
        hulls.First().HullType.Should().Be("Template");
    }

    [Fact]
    public async Task GetHull_ValidId_ReturnsHullDetails()
    {
        // Act
        var result = await _controller.GetHull(_testHull.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var hull = okResult.Value.Should().BeAssignableTo<CatalogHullDto>().Subject;

        hull.Id.Should().Be(_testHull.Id);
        hull.Slug.Should().Be("test-hull");
        hull.Title.Should().Be("Test Hull");
        hull.Lpp.Should().Be(100m);
        hull.Beam.Should().Be(20m);
        hull.Draft.Should().Be(10m);
        hull.Cb.Should().Be(0.7m);
        hull.GeometryMissing.Should().BeFalse();

        // Check LCB/LCF calculation (percentage to meters)
        hull.LCB.Should().Be(2m); // 100m * 2% / 100 = 2m
        hull.LCF.Should().Be(-1.5m); // 100m * -1.5% / 100 = -1.5m
    }

    [Fact]
    public async Task GetHull_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetHull(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CloneHull_ValidHullWithGeometry_CreatesVessel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedVessel = new Vessel
        {
            Id = Guid.NewGuid(),
            Name = "Cloned Test Hull",
            Lpp = 100m,
            Beam = 20m,
            DesignDraft = 10m,
            UserId = userId
        };

        _vesselServiceMock
            .Setup(s => s.CreateVesselAsync(
                It.IsAny<VesselDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((VesselDto dto, Guid uid, CancellationToken ct) =>
            {
                // Add and save the vessel to the context so it exists in the "database"
                _context.Vessels.Add(expectedVessel);
                _context.SaveChanges();
                // Detach so the controller can re-attach with Update()
                _context.Entry(expectedVessel).State = EntityState.Detached;
                return expectedVessel;
            });

        var request = new CatalogHullsController.CloneHullRequestDto
        {
            VesselName = "Cloned Test Hull",
            UserId = userId
        };

        // Act
        var result = await _controller.CloneHull(_testHull.Id, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<CatalogHullsController.CloneHullResponseDto>().Subject;

        response.VesselId.Should().Be(expectedVessel.Id);
        response.VesselName.Should().Be("Cloned Test Hull");
        response.Message.Should().Contain("Successfully cloned");

        // Verify the vessel service was called
        _vesselServiceMock.Verify(s => s.CreateVesselAsync(
            It.Is<VesselDto>(dto =>
                dto.Name == "Cloned Test Hull" &&
                dto.Lpp == 100m &&
                dto.Beam == 20m &&
                dto.DesignDraft == 10m),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify the vessel was updated with catalog reference
        var updatedVessel = await _context.Vessels.FindAsync(expectedVessel.Id);
        updatedVessel.Should().NotBeNull();
        updatedVessel!.SourceCatalogHullId.Should().Be(_testHull.Id);
    }

    [Fact]
    public async Task CloneHull_HullWithoutGeometry_ReturnsBadRequest()
    {
        // Arrange
        var hullWithoutGeometry = await _context.BenchmarkCases
            .FirstAsync(h => h.Slug == "hull-no-geometry");

        var request = new CatalogHullsController.CloneHullRequestDto
        {
            VesselName = "Should Fail"
        };

        // Act
        var result = await _controller.CloneHull(hullWithoutGeometry.Id, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CloneHull_InvalidHullId_ReturnsNotFound()
    {
        // Arrange
        var request = new CatalogHullsController.CloneHullRequestDto
        {
            VesselName = "Test"
        };

        // Act
        var result = await _controller.CloneHull(Guid.NewGuid(), request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetHullGeometry_ValidHullWithGeometry_ReturnsGeometry()
    {
        // Act
        var result = await _controller.GetHullGeometry(_testHull.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var geometry = okResult.Value.Should().BeAssignableTo<CatalogHullsController.CatalogGeometryDto>().Subject;

        geometry.StationsJson.Should().NotBeNullOrEmpty();
        geometry.WaterlinesJson.Should().NotBeNullOrEmpty();
        geometry.OffsetsJson.Should().NotBeNullOrEmpty();
        geometry.Type.Should().Be("Offsets");
        geometry.SourceUrl.Should().Be("https://test.com/geometry");
    }

    [Fact]
    public async Task GetHullGeometry_HullWithoutGeometry_ReturnsNotFound()
    {
        // Arrange
        var hullWithoutGeometry = await _context.BenchmarkCases
            .FirstAsync(h => h.Slug == "hull-no-geometry");

        // Act
        var result = await _controller.GetHullGeometry(hullWithoutGeometry.Id);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetHullGeometry_InvalidHullId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetHullGeometry(Guid.NewGuid());

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
