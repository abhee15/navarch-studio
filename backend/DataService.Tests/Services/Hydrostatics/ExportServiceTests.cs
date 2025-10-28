using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs;
using Shared.Models;
using Xunit;
using FluentAssertions;

namespace DataService.Tests.Services.Hydrostatics;

public class ExportServiceTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly Mock<ILogger<ExportService>> _mockLogger;
    private readonly Mock<IHydroCalculator> _mockCalculator;
    private readonly Mock<ICurvesGenerator> _mockCurvesGenerator;
    private readonly ExportService _exportService;

    public ExportServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new DataDbContext(options);

        _mockLogger = new Mock<ILogger<ExportService>>();
        _mockCalculator = new Mock<IHydroCalculator>();
        _mockCurvesGenerator = new Mock<ICurvesGenerator>();

        _exportService = new ExportService(
            _mockCalculator.Object,
            _mockCurvesGenerator.Object,
            _context,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldReturnValidCsv()
    {
        // Arrange
        var results = CreateSampleHydroResults();

        // Act
        var csvData = await _exportService.ExportToCsvAsync(results);

        // Assert
        csvData.Should().NotBeNull();
        csvData.Length.Should().BeGreaterThan(0);

        var csvContent = System.Text.Encoding.UTF8.GetString(csvData);
        csvContent.Should().Contain("Draft (m)");
        csvContent.Should().Contain("Displacement (kg)");
        csvContent.Should().Contain("KB (m)");
        csvContent.Should().Contain("5.000"); // Draft value from sample data
    }

    [Fact]
    public async Task ExportToJsonAsync_ShouldReturnValidJson()
    {
        // Arrange
        var results = CreateSampleHydroResults();

        // Act
        var jsonData = await _exportService.ExportToJsonAsync(results);

        // Assert
        jsonData.Should().NotBeNull();
        jsonData.Length.Should().BeGreaterThan(0);

        var jsonContent = System.Text.Encoding.UTF8.GetString(jsonData);
        jsonContent.Should().Contain("\"draft\":");
        jsonContent.Should().Contain("\"dispWeight\":");
        jsonContent.Should().Contain("\"kBz\":");
    }

    [Fact]
    public async Task ExportToPdfAsync_ShouldGeneratePdfForValidVessel()
    {
        // Arrange
        var vessel = CreateTestVessel();
        _context.Vessels.Add(vessel);
        await _context.SaveChangesAsync();

        var results = CreateSampleHydroResults();
        _mockCalculator.Setup(x => x.ComputeTableAsync(
            vessel.Id,
            null,
            It.IsAny<List<decimal>>(),
            default))
            .ReturnsAsync(results);

        // Act
        var pdfData = await _exportService.ExportToPdfAsync(vessel.Id, null, false);

        // Assert
        pdfData.Should().NotBeNull();
        pdfData.Length.Should().BeGreaterThan(1000); // PDF should be at least 1KB

        // Check PDF magic bytes
        var header = System.Text.Encoding.ASCII.GetString(pdfData.Take(4).ToArray());
        header.Should().Be("%PDF"); // PDF files start with %PDF
    }

    [Fact]
    public async Task ExportToExcelAsync_ShouldGenerateExcelForValidVessel()
    {
        // Arrange
        var vessel = CreateTestVessel();
        _context.Vessels.Add(vessel);
        await _context.SaveChangesAsync();

        var results = CreateSampleHydroResults();
        _mockCalculator.Setup(x => x.ComputeTableAsync(
            vessel.Id,
            null,
            It.IsAny<List<decimal>>(),
            default))
            .ReturnsAsync(results);

        // Act
        var excelData = await _exportService.ExportToExcelAsync(vessel.Id, null, false);

        // Assert
        excelData.Should().NotBeNull();
        excelData.Length.Should().BeGreaterThan(1000); // Excel should be at least 1KB

        // Check ZIP magic bytes (Excel files are ZIP archives)
        var header = excelData.Take(2).ToArray();
        header.Should().BeEquivalentTo(new byte[] { 0x50, 0x4B }); // PK signature
    }

    [Fact]
    public async Task ExportToPdfAsync_WithCurves_ShouldAttemptToGenerateCurves()
    {
        // Arrange
        var vessel = CreateTestVessel();
        _context.Vessels.Add(vessel);
        await _context.SaveChangesAsync();

        var results = CreateSampleHydroResults();
        _mockCalculator.Setup(x => x.ComputeTableAsync(
            vessel.Id,
            null,
            It.IsAny<List<decimal>>(),
            default))
            .ReturnsAsync(results);

        // Mock curve generation (returns empty dictionary if curves fail to generate)
        _mockCurvesGenerator.Setup(x => x.GenerateMultipleCurvesAsync(
            vessel.Id,
            null,
            It.IsAny<List<string>>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<int>(),
            default))
            .ReturnsAsync(new Dictionary<string, CurveDto>());

        // Act
        var pdfData = await _exportService.ExportToPdfAsync(vessel.Id, null, true);

        // Assert
        pdfData.Should().NotBeNull();
        pdfData.Length.Should().BeGreaterThan(1000);

        // PDF should still be generated even if curves fail
        var header = System.Text.Encoding.ASCII.GetString(pdfData.Take(4).ToArray());
        header.Should().Be("%PDF");
    }

    [Fact]
    public async Task ExportToPdfAsync_VesselNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentVesselId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _exportService.ExportToPdfAsync(nonExistentVesselId, null, false);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Vessel {nonExistentVesselId} not found");
    }

    [Fact]
    public async Task ExportToExcelAsync_VesselNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentVesselId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _exportService.ExportToExcelAsync(nonExistentVesselId, null, false);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Vessel {nonExistentVesselId} not found");
    }

    [Fact]
    public async Task ExportCurvesToCsvAsync_ShouldReturnValidCsv()
    {
        // Arrange
        var curves = CreateSampleCurves();

        // Act
        var csvData = await _exportService.ExportCurvesToCsvAsync(curves);

        // Assert
        csvData.Should().NotBeNull();
        csvData.Length.Should().BeGreaterThan(0);

        var csvContent = System.Text.Encoding.UTF8.GetString(csvData);
        csvContent.Should().Contain("# displacement Curve");
        csvContent.Should().Contain("Draft (m)");
        csvContent.Should().Contain("Displacement (kg)");
    }

    [Fact]
    public async Task ExportCurvesToJsonAsync_ShouldReturnValidJson()
    {
        // Arrange
        var curves = CreateSampleCurves();

        // Act
        var jsonData = await _exportService.ExportCurvesToJsonAsync(curves);

        // Assert
        jsonData.Should().NotBeNull();
        jsonData.Length.Should().BeGreaterThan(0);

        var jsonContent = System.Text.Encoding.UTF8.GetString(jsonData);
        jsonContent.Should().Contain("\"type\":");
        jsonContent.Should().Contain("\"xLabel\":");
        jsonContent.Should().Contain("\"yLabel\":");
        jsonContent.Should().Contain("\"points\":");
    }

    // Helper methods
    private static Vessel CreateTestVessel()
    {
        return new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Test Vessel",
            Description = "A test vessel for export tests",
            Lpp = 100m,
            Beam = 20m,
            DesignDraft = 10m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static List<HydroResultDto> CreateSampleHydroResults()
    {
        return new List<HydroResultDto>
        {
            new()
            {
                Draft = 5.0m,
                DispVolume = 5000m,
                DispWeight = 5125000m,
                KBz = 2.5m,
                LCBx = 50m,
                TCBy = 0m,
                BMt = 8.5m,
                BMl = 150m,
                GMt = 6.0m,
                GMl = 147.5m,
                Awp = 1800m,
                Iwp = 25000m,
                Cb = 0.65m,
                Cp = 0.72m,
                Cm = 0.90m,
                Cwp = 0.90m
            },
            new()
            {
                Draft = 6.0m,
                DispVolume = 6200m,
                DispWeight = 6355000m,
                KBz = 3.0m,
                LCBx = 50m,
                TCBy = 0m,
                BMt = 7.8m,
                BMl = 145m,
                GMt = 5.8m,
                GMl = 145m,
                Awp = 1850m,
                Iwp = 28000m,
                Cb = 0.68m,
                Cp = 0.74m,
                Cm = 0.92m,
                Cwp = 0.92m
            }
        };
    }

    private static List<CurveDto> CreateSampleCurves()
    {
        return new List<CurveDto>
        {
            new()
            {
                Type = "displacement",
                XLabel = "Draft (m)",
                YLabel = "Displacement (kg)",
                Points = new List<CurvePointDto>
                {
                    new() { X = 3m, Y = 3000000m },
                    new() { X = 4m, Y = 4000000m },
                    new() { X = 5m, Y = 5000000m }
                }
            },
            new()
            {
                Type = "kb",
                XLabel = "Draft (m)",
                YLabel = "KB (m)",
                Points = new List<CurvePointDto>
                {
                    new() { X = 3m, Y = 1.5m },
                    new() { X = 4m, Y = 2.0m },
                    new() { X = 5m, Y = 2.5m }
                }
            }
        };
    }
}

