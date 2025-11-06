using DataService.Data;
using DataService.Services.Catalog;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataService.Tests.Services.Catalog;

public class VesselCatalogImporterTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly VesselCatalogImporter _importer;
    private readonly Mock<ILogger<VesselCatalogImporter>> _loggerMock;

    public VesselCatalogImporterTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _loggerMock = new Mock<ILogger<VesselCatalogImporter>>();
        _importer = new VesselCatalogImporter(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task ImportFromCsvAsync_ValidData_ImportsSuccessfully()
    {
        // Arrange
        var csvContent = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
KCS,Container,230.0,32.2,10.8,19.0,52030.0,0.6505,0.66,0.9849,0.83,12.34,50000,Diesel,2002,SIMMAN,Model tests,IGES,";

        // Act
        var result = await _importer.ImportFromCsvAsync(csvContent);

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedRows.Should().Be(1);
        result.SkippedRows.Should().Be(0);

        var vessel = await _context.CatalogVesselsReal.FirstOrDefaultAsync();
        vessel.Should().NotBeNull();
        vessel!.VesselId.Should().Be("KCS");
        vessel.VesselType.Should().Be("Container");
        vessel.LppM.Should().Be(230.0m);
        vessel.BeamM.Should().Be(32.2m);
        vessel.Cb.Should().Be(0.6505m);
        vessel.IsSystemData.Should().BeTrue();
        vessel.CreatedBy.Should().BeNull();
    }

    [Fact]
    public async Task ImportFromCsvAsync_MissingDepth_EstimatesFromDraft()
    {
        // Arrange
        var csvContent = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
TEST01,Container,100.0,15.0,6.0,,10000.0,0.65,0.70,0.93,0.80,10.0,8000,Diesel,2020,Test,Design,,";

        // Act
        var result = await _importer.ImportFromCsvAsync(csvContent);

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedRows.Should().Be(1);
        result.Warnings.Should().Contain(w => w.Contains("Estimated Depth"));

        var vessel = await _context.CatalogVesselsReal.FirstOrDefaultAsync();
        vessel!.DepthM.Should().Be(9.0m);  // 6.0 * 1.5
    }

    [Fact]
    public async Task ImportFromCsvAsync_MissingRequiredField_SkipsRow()
    {
        // Arrange
        var csvContent = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
INVALID,,100.0,15.0,6.0,9.0,10000.0,0.65,,,,,,,,,";  // Missing VesselType

        // Act
        var result = await _importer.ImportFromCsvAsync(csvContent);

        // Assert
        result.SkippedRows.Should().Be(1);
        result.ImportedRows.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("VesselType is required"));
    }

    [Fact]
    public async Task ImportFromCsvAsync_InvalidCbRange_SkipsRow()
    {
        // Arrange
        var csvContent = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
INVALID,Container,100.0,15.0,6.0,9.0,10000.0,1.5,,,,,,,,,";  // CB > 0.95

        // Act
        var result = await _importer.ImportFromCsvAsync(csvContent);

        // Assert
        result.SkippedRows.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("CB must be between"));
    }

    [Fact]
    public async Task ImportFromCsvAsync_DuplicateVesselId_SkipsSecond()
    {
        // Arrange
        var csvContent1 = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
TEST01,Container,100.0,15.0,6.0,9.0,10000.0,0.65,0.70,0.93,0.80,10.0,8000,Diesel,2020,Test,Design,,";

        var csvContent2 = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
TEST01,Tanker,200.0,30.0,12.0,18.0,50000.0,0.80,0.82,0.98,0.85,8.0,45000,Diesel,2021,Test,Design,,";

        // Act
        await _importer.ImportFromCsvAsync(csvContent1);
        var result2 = await _importer.ImportFromCsvAsync(csvContent2);

        // Assert
        result2.SkippedRows.Should().Be(1);
        result2.ImportedRows.Should().Be(0);

        var count = await _context.CatalogVesselsReal.CountAsync();
        count.Should().Be(1);  // Only first import succeeded
    }

    [Fact]
    public async Task ImportFromCsvAsync_MultipleVessels_ImportsAll()
    {
        // Arrange
        var csvContent = @"VesselID,VesselType,Lpp_m,Beam_m,Draft_m,Depth_m,Displacement_t,CB,CP,CM,CW,ServiceSpeed_ms,DWT_t,EngineType,YearBuilt,Source,DataQuality,HullGeometryFile,ResistanceCurve
KCS,Container,230.0,32.2,10.8,19.0,52030.0,0.6505,0.66,0.9849,0.83,12.34,50000,Diesel,2002,SIMMAN,Model tests,IGES,
KVLCC2,Tanker,320.0,58.0,20.8,30.0,312622.0,0.8098,0.82,0.998,0.88,15.5,280000,Diesel,2000,SIMMAN,Model tests,IGES,
DTMB_5415,Naval combatant,142.0,17.983,6.179,9.3,12901.6,0.506,0.55,0.90,0.778,10.0,5000,Diesel,1970,DTMB,Model tests,IGES,";

        // Act
        var result = await _importer.ImportFromCsvAsync(csvContent);

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedRows.Should().Be(3);
        result.SkippedRows.Should().Be(0);

        var count = await _context.CatalogVesselsReal.CountAsync();
        count.Should().Be(3);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

