using DataService.Services.Resistance;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs;
using Xunit;

namespace DataService.Tests.Services;

public class DefaultValuesServiceTests
{
    private readonly Mock<ILogger<DefaultValuesService>> _loggerMock;
    private readonly DefaultValuesService _service;

    public DefaultValuesServiceTests()
    {
        _loggerMock = new Mock<ILogger<DefaultValuesService>>();
        _service = new DefaultValuesService(_loggerMock.Object);
    }

    [Fact]
    public void GetDefaultValues_WithTankerType_ReturnsLowFormFactor()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "Tanker"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FormFactor);
        Assert.Equal(0.15m, result.FormFactor.Value);
        Assert.Contains("Tanker", result.FormFactor.Provenance);
    }

    [Fact]
    public void GetDefaultValues_WithContainerShipType_ReturnsHigherFormFactor()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "ContainerShip"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FormFactor);
        Assert.Equal(0.22m, result.FormFactor.Value);
    }

    [Fact]
    public void GetDefaultValues_WithHighCB_ReturnsTankerDefaults()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            CB = 0.80m // High block coefficient
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FormFactor);
        // Should infer Tanker type from high CB
        Assert.Contains("Tanker", result.Provenance);
    }

    [Fact]
    public void GetDefaultValues_WithLowCB_ReturnsHigherFormFactor()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            CB = 0.55m // Low block coefficient (fine-lined vessel)
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FormFactor);
        // Fine-lined vessels should have higher form factor
        Assert.True(result.FormFactor.Value > 0.20m);
    }

    [Fact]
    public void GetDefaultValues_ReturnsAllEfficiencies()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "CargoShip"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result.EtaD);
        Assert.NotNull(result.EtaH);
        Assert.NotNull(result.EtaR);
        Assert.NotNull(result.EtaO);

        // Overall efficiency should be in reasonable range
        Assert.InRange(result.EtaD.Value, 0.5m, 0.8m);
    }

    [Fact]
    public void GetDefaultValues_ReturnsAppendageArea()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "Ferry"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result.AppendageAreaPercent);
        // Ferry should have relatively high appendage area
        Assert.True(result.AppendageAreaPercent.Value > 1.0m);
    }

    [Fact]
    public void GetDefaultValues_ReturnsRoughnessAllowance()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "General"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result.RoughnessAllowance);
        Assert.Equal(0.0004m, result.RoughnessAllowance.Value);
        Assert.Contains("ITTC-1978", result.RoughnessAllowance.Provenance);
    }

    [Fact]
    public void GetDefaultValues_WithDimensions_EstimatesWettedSurfaceArea()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "CargoShip",
            Lpp = 100m,
            Beam = 20m,
            Draft = 10m
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotNull(result.WettedSurfaceArea);
        Assert.True(result.WettedSurfaceArea.Value > 0);
        Assert.Contains("ITTC formula", result.WettedSurfaceArea.Provenance);
    }

    [Fact]
    public void GetDefaultValues_WithoutDimensions_DoesNotEstimateWettedSurfaceArea()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "CargoShip"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.Null(result.WettedSurfaceArea);
    }

    [Fact]
    public void GetDefaultValues_AllProvenanceStringsAreSet()
    {
        // Arrange
        var request = new DefaultValuesRequestDto
        {
            VesselType = "Yacht"
        };

        // Act
        var result = _service.GetDefaultValues(request);

        // Assert
        Assert.NotEmpty(result.Provenance);
        Assert.NotEmpty(result.FormFactor!.Provenance);
        Assert.NotEmpty(result.AppendageAreaPercent!.Provenance);
        Assert.NotEmpty(result.RoughnessAllowance!.Provenance);
        Assert.NotEmpty(result.EtaD!.Provenance);
        Assert.NotEmpty(result.EtaH!.Provenance);
        Assert.NotEmpty(result.EtaR!.Provenance);
        Assert.NotEmpty(result.EtaO!.Provenance);
    }

    [Fact]
    public void GetDefaultValues_TankerHasHigherEtaD()
    {
        // Arrange
        var tankerRequest = new DefaultValuesRequestDto { VesselType = "Tanker" };
        var ferryRequest = new DefaultValuesRequestDto { VesselType = "Ferry" };

        // Act
        var tankerResult = _service.GetDefaultValues(tankerRequest);
        var ferryResult = _service.GetDefaultValues(ferryRequest);

        // Assert
        // Tankers (slow, efficient) should have higher ηD than ferries (fast)
        Assert.True(tankerResult.EtaD!.Value > ferryResult.EtaD!.Value);
    }
}

