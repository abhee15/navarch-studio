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
    private CatalogVesselReal _testVessel = null!;

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
        _testVessel = new CatalogVesselReal
        {
            Id = Guid.NewGuid(),
            VesselId = "test-hull",
            VesselType = "Template",
            LppM = 100m,
            BeamM = 20m,
            DraftM = 10m,
            DisplacementT = 5000m,
            Cb = 0.7m,
            Cp = 0.75m,
            Cm = 0.93m,
            DataQuality = "Good",
            Source = "Test Data",
            IsSystemData = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CatalogVesselsReal.Add(_testVessel);

        // Add another vessel with different type
        var vesselContainer = new CatalogVesselReal
        {
            Id = Guid.NewGuid(),
            VesselId = "container-vessel",
            VesselType = "Container",
            LppM = 200m,
            BeamM = 30m,
            DraftM = 15m,
            DisplacementT = 20000m,
            Cb = 0.65m,
            DataQuality = "Fair",
            Source = "Test Data",
            IsSystemData = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CatalogVesselsReal.Add(vesselContainer);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ListHulls_NoFilter_ReturnsAllVessels()
    {
        // Act
        var result = await _controller.ListHulls();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var vessels = okResult.Value.Should().BeAssignableTo<List<CatalogHullsController.RealVesselDto>>().Subject;
        vessels.Should().HaveCount(2);
        vessels.Should().Contain(v => v.Name == "test-hull");
        vessels.Should().Contain(v => v.Name == "container-vessel");
    }

    [Fact]
    public async Task ListHulls_FilterByType_ReturnsFilteredHulls()
    {
        // Act
        var result = await _controller.ListHulls(vesselType: "Template");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var hulls = okResult.Value.Should().BeAssignableTo<List<CatalogHullsController.RealVesselDto>>().Subject;
        hulls.Should().HaveCount(1);
        hulls.First().Name.Should().Be("test-hull");
        hulls.First().VesselType.Should().Be("Template");
    }

    [Fact]
    public async Task GetHull_ValidId_ReturnsVesselDetails()
    {
        // Act
        var result = await _controller.GetHull(_testVessel.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var vessel = okResult.Value.Should().BeAssignableTo<CatalogHullsController.RealVesselDetailsDto>().Subject;

        vessel.Id.Should().Be(_testVessel.Id.ToString());
        vessel.Name.Should().Be("test-hull");
        vessel.VesselType.Should().Be("Template");
        vessel.Lpp.Should().Be(100m);
        vessel.Beam.Should().Be(20m);
        vessel.Draft.Should().Be(10m);
        vessel.Cb.Should().Be(0.7m);
        vessel.Cp.Should().Be(0.75m);
        vessel.Cm.Should().Be(0.93m);
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
        var result = await _controller.CloneHull(_testVessel.Id, request);

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
        updatedVessel!.SourceCatalogHullId.Should().Be(_testVessel.Id);
    }

    [Fact]
    public async Task CloneHull_InvalidVesselId_ReturnsNotFound()
    {
        // Arrange
        var request = new CatalogHullsController.CloneHullRequestDto
        {
            VesselName = "Should Fail"
        };

        // Act
        var result = await _controller.CloneHull(Guid.NewGuid(), request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetHullGeometry_InvalidVesselId_ReturnsNotFound()
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
