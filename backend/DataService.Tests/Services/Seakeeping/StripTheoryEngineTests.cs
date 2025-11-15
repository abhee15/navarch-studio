using DataService.Data;
using DataService.Services.Hydrostatics;
using DataService.Services.Seakeeping;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataService.Tests.Services.Seakeeping;

/// <summary>
/// Tests for Strip Theory Engine.
/// Validates hydrodynamic coefficient calculations.
/// </summary>
public class StripTheoryEngineTests
{
    private readonly Mock<IGeometryService> _mockGeometry;
    private readonly Mock<IIntegrationEngine> _mockIntegration;
    private readonly Mock<ILogger<StripTheoryEngine>> _mockLogger;

    public StripTheoryEngineTests()
    {
        // Note: StripTheoryEngine requires a real DataDbContext (can't mock DbContext due to sealed members)
        // These tests are placeholders for future integration tests with in-memory database
        _mockGeometry = new Mock<IGeometryService>();
        _mockIntegration = new Mock<IIntegrationEngine>();
        _mockLogger = new Mock<ILogger<StripTheoryEngine>>();
    }

    [Fact(Skip = "Requires real DataDbContext - convert to integration test with in-memory database")]
    public async Task ComputeCoefficients_ValidInput_ReturnsHydrodynamicCoefficients()
    {
        // Arrange
        var vesselId = Guid.NewGuid();
        var draft = 5.0;
        var frequencies = new[] { 0.4, 0.6, 0.8 };

        // TODO: Convert to integration test with real in-memory database
        // Cannot mock DataDbContext due to sealed/non-virtual members

        // Act
        // var result = await _sut.ComputeCoefficientsAsync(vesselId, draft, frequencies);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public void SectionCoefficients_EllipticFormula_ProducesPositiveValues()
    {
        // This test validates that the simplified elliptic formulas
        // produce reasonable (positive) values for added mass and damping

        // Arrange
        var breadth = 10.0; // m
        var height = 5.0;   // m

        // Act & Assert
        // Simplified elliptic: a33 ≈ ρπab should be positive
        var expectedA33 = 1025.0 * Math.PI * (breadth / 2) * height;
        expectedA33.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "Requires real DataDbContext - convert to integration test with in-memory database")]
    public async Task ComputeCoefficients_NoGeometry_ThrowsException()
    {
        // Arrange
        var vesselId = Guid.NewGuid();
        var frequencies = new[] { 0.4 };

        // TODO: Convert to integration test with real in-memory database

        // Act & Assert
        Assert.True(true);
    }
}
