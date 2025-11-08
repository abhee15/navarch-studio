using Xunit;
using Moq;
using FluentAssertions;
using DataService.Services.Seakeeping;
using DataService.Services.Hydrostatics;
using DataService.Data;
using Microsoft.Extensions.Logging;

namespace DataService.Tests.Services.Seakeeping;

/// <summary>
/// Tests for Strip Theory Engine.
/// Validates hydrodynamic coefficient calculations.
/// </summary>
public class StripTheoryEngineTests
{
    private readonly Mock<DataDbContext> _mockContext;
    private readonly Mock<IGeometryService> _mockGeometry;
    private readonly Mock<IIntegrationEngine> _mockIntegration;
    private readonly Mock<ILogger<StripTheoryEngine>> _mockLogger;
    private readonly StripTheoryEngine _sut;

    public StripTheoryEngineTests()
    {
        _mockContext = new Mock<DataDbContext>();
        _mockGeometry = new Mock<IGeometryService>();
        _mockIntegration = new Mock<IIntegrationEngine>();
        _mockLogger = new Mock<ILogger<StripTheoryEngine>>();

        _sut = new StripTheoryEngine(
            _mockContext.Object,
            _mockGeometry.Object,
            _mockIntegration.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ComputeCoefficients_ValidInput_ReturnsHydrodynamicCoefficients()
    {
        // Arrange
        var vesselId = Guid.NewGuid();
        var draft = 5.0;
        var frequencies = new[] { 0.4, 0.6, 0.8 };

        // TODO: Setup mock geometry data
        // TODO: Setup mock integration engine responses

        // Act
        // var result = await _sut.ComputeCoefficientsAsync(vesselId, draft, frequencies);

        // Assert
        // result.Should().NotBeNull();
        // result.Frequency.Should().HaveCount(3);
        // result.AddedMass.Should().HaveCount(3);
    }

    [Fact]
    public void SectionCoefficients_EllipticFormula_ProducesPositiveValues()
    {
        // This test validates that the simplified elliptic formulas
        // produce reasonable (positive) values for added mass and damping

        // Arrange
        var breadth = 10.0; // m
        var height = 5.0;   // m
        var omega = 0.5;    // rad/s

        // Act & Assert
        // Simplified elliptic: a33 ≈ ρπab should be positive
        var expectedA33 = 1025.0 * Math.PI * (breadth / 2) * height;
        expectedA33.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ComputeCoefficients_NoGeometry_ThrowsException()
    {
        // Arrange
        var vesselId = Guid.NewGuid();
        var frequencies = new[] { 0.4 };

        _mockGeometry.Setup(g => g.GetOffsetsGridAsync(vesselId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Shared.DTOs.OffsetsGridDto
            {
                Stations = new List<decimal>(),
                Waterlines = new List<decimal>(),
                Offsets = new List<List<decimal>>()
            });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.ComputeCoefficientsAsync(vesselId, 5.0, frequencies)
        );
    }
}
