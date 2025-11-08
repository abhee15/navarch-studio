using DataService.Services.Seakeeping;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NavArch.Shared.DTOs;
using Xunit;

namespace DataService.Tests.Services.Seakeeping;

/// <summary>
/// Tests for Wave Spectrum Service.
/// Validates JONSWAP and PM spectrum calculations.
/// </summary>
public class WaveSpectrumServiceTests
{
    private readonly WaveSpectrumService _sut;

    public WaveSpectrumServiceTests()
    {
        var mockLogger = new Mock<ILogger<WaveSpectrumService>>();
        _sut = new WaveSpectrumService(mockLogger.Object);
    }

    [Fact]
    public void ComputeSpectrum_JONSWAP_ReturnsValidSpectrum()
    {
        // Arrange
        var seaState = new SeaStateDto(
            SignificantHeight: 3.0,
            PeakPeriod: 8.0,
            Heading: 180,
            Spectrum: "JONSWAP",
            Gamma: 3.3
        );
        var frequencies = Enumerable.Range(0, 50)
            .Select(i => 0.2 + i * 0.05)
            .ToArray();

        // Act
        var spectrum = _sut.ComputeSpectrum(seaState, frequencies);

        // Assert
        spectrum.Should().NotBeNull();
        spectrum.Should().HaveCount(frequencies.Length);
        spectrum.All(s => s >= 0).Should().BeTrue(); // All values should be non-negative

        // Spectrum should have a peak near ωₚ = 2π/Tp
        var omegaP = 2.0 * Math.PI / seaState.PeakPeriod;
        var peakValue = spectrum.Max();
        var peakIdx = Array.IndexOf(spectrum, peakValue);
        var peakFreq = frequencies[peakIdx];

        // Peak should be near theoretical peak frequency (within ±20%)
        peakFreq.Should().BeApproximately(omegaP, omegaP * 0.2);
    }

    [Fact]
    public void ComputeSpectrum_PM_ReturnsValidSpectrum()
    {
        // Arrange
        var seaState = new SeaStateDto(
            SignificantHeight: 2.5,
            PeakPeriod: 7.0,
            Heading: 180,
            Spectrum: "PM",
            Gamma: 1.0 // PM uses γ=1
        );
        var frequencies = Enumerable.Range(0, 40)
            .Select(i => 0.3 + i * 0.05)
            .ToArray();

        // Act
        var spectrum = _sut.ComputeSpectrum(seaState, frequencies);

        // Assert
        spectrum.Should().NotBeNull();
        spectrum.Should().HaveCount(frequencies.Length);
        spectrum.All(s => s >= 0).Should().BeTrue();
    }

    [Theory]
    [InlineData("JONSWAP", 3.0, 8.0, 3.3)]
    [InlineData("PM", 2.0, 6.0, 1.0)]
    [InlineData("JONSWAP", 5.0, 12.0, 2.5)]
    public void ComputeSpectrum_VariousSeaStates_ProducesValidShape(
        string spectrum,
        double hs,
        double tp,
        double gamma)
    {
        // Arrange
        var seaState = new SeaStateDto(hs, tp, 180, spectrum, gamma);
        var frequencies = Enumerable.Range(0, 60)
            .Select(i => 0.2 + i * 0.05)
            .ToArray();

        // Act
        var result = _sut.ComputeSpectrum(seaState, frequencies);

        // Assert - Spectrum should decay at high frequencies
        var highFreqAvg = result[^10..].Average(); // Last 10 points
        var peakValue = result.Max();

        highFreqAvg.Should().BeLessThan(peakValue * 0.01); // High freq should be <1% of peak
    }

    [Fact]
    public void ComputeSpectrum_InvalidSpectrum_ThrowsException()
    {
        // Arrange
        var seaState = new SeaStateDto(3.0, 8.0, 180, "INVALID", 3.3);
        var frequencies = new[] { 0.5 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _sut.ComputeSpectrum(seaState, frequencies)
        );
    }
}

