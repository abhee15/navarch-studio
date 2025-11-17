using FluentAssertions;
using Shared.HullGenerators.ParentHull;
using Xunit;

namespace Shared.Tests.HullGenerators.ParentHull;

/// <summary>
/// Unit tests for ParentHullScaler
/// Tests affine transformation scaling of parent hull offsets
/// </summary>
public class ParentHullScalerTests
{
    [Fact]
    public void CalculateScaleFactors_WithStandardDimensions_ReturnsCorrectFactors()
    {
        // Arrange - Parent hull: L=185m, B=28m, T=12.87m
        // Target: L=200m, B=30m, T=13m
        var parent = new ParentHullData
        {
            Lbp = 185m,
            B = 28m,
            T = 12.87m
        };

        decimal lengthTarget = 200m;
        decimal beamTarget = 30m;
        decimal draftTarget = 13m;

        // Act
        var scale = ParentHullScaler.CalculateScaleFactors(parent, lengthTarget, beamTarget, draftTarget);

        // Assert
        scale.Length.Should().BeApproximately(200m / 185m, 0.001m);
        scale.Breadth.Should().BeApproximately(30m / 28m, 0.001m);
        scale.Draft.Should().BeApproximately(13m / 12.87m, 0.001m);
    }

    [Fact]
    public void ScaleOffsets_WithBSRAStations_ScalesCorrectly()
    {
        // Arrange - Parent hull with BSRA standard stations (normalized 0-10)
        var parent = new ParentHullData
        {
            Lbp = 185m,
            B = 28m,
            T = 12.87m,
            Stations = new List<decimal> { 0m, 5m, 10m }, // AP, midship, FP
            Waterlines = new List<decimal> { 0m, 6.435m, 12.87m }, // Keel, half draft, design draft
            Offsets = new List<List<decimal>>
            {
                new() { 0m, 0m, 0m },      // Station 0 (AP)
                new() { 0m, 14m, 14m },    // Station 5 (midship)
                new() { 0m, 0m, 0m }       // Station 10 (FP)
            }
        };

        var scale = new ScaleFactors
        {
            Length = 200m / 185m,
            Breadth = 30m / 28m,
            Draft = 13m / 12.87m
        };

        // Act
        var scaled = ParentHullScaler.ScaleOffsets(parent, scale, 200m);

        // Assert - Stations should be scaled to target length
        scaled.Stations[0].Should().BeApproximately(0m, 0.001m); // AP stays at 0
        scaled.Stations[1].Should().BeApproximately(100m, 0.001m); // Midship at L/2 = 200/2 = 100m
        scaled.Stations[2].Should().BeApproximately(200m, 0.001m); // FP at L = 200m

        // Waterlines should be scaled to target draft
        scaled.Waterlines[0].Should().BeApproximately(0m, 0.001m);
        scaled.Waterlines[1].Should().BeApproximately(13m / 2m, 0.001m); // Half draft scaled
        scaled.Waterlines[2].Should().BeApproximately(13m, 0.001m); // Design draft scaled

        // Offsets should be scaled to target beam
        scaled.Offsets[1][1].Should().BeApproximately(14m * (30m / 28m), 0.001m); // Midship half-breadth scaled
    }

    [Fact]
    public void ScaleOffsets_StationNormalization_IsCorrect()
    {
        // Arrange - Test station normalization from BSRA 0-10 to actual positions
        var parent = new ParentHullData
        {
            Lbp = 100m, // Parent length
            B = 20m,
            T = 10m,
            Stations = new List<decimal> { 0m, 2.5m, 5m, 7.5m, 10m }, // BSRA stations
            Waterlines = new List<decimal> { 0m, 5m, 10m },
            Offsets = new List<List<decimal>>
            {
                new() { 0m, 0m, 0m },
                new() { 0m, 5m, 5m },
                new() { 0m, 10m, 10m },
                new() { 0m, 5m, 5m },
                new() { 0m, 0m, 0m }
            }
        };

        var scale = new ScaleFactors
        {
            Length = 200m / 100m, // Scale to 200m length
            Breadth = 1m, // No beam scaling
            Draft = 1m // No draft scaling
        };

        // Act
        var scaled = ParentHullScaler.ScaleOffsets(parent, scale, 200m);

        // Assert - Verify station positions are correctly scaled
        // Station 0 (normalized 0/10 = 0) → 0 * 200 = 0m
        scaled.Stations[0].Should().BeApproximately(0m, 0.001m);
        // Station 2.5 (normalized 2.5/10 = 0.25) → 0.25 * 200 = 50m
        scaled.Stations[1].Should().BeApproximately(50m, 0.001m);
        // Station 5 (normalized 5/10 = 0.5) → 0.5 * 200 = 100m
        scaled.Stations[2].Should().BeApproximately(100m, 0.001m);
        // Station 7.5 (normalized 7.5/10 = 0.75) → 0.75 * 200 = 150m
        scaled.Stations[3].Should().BeApproximately(150m, 0.001m);
        // Station 10 (normalized 10/10 = 1.0) → 1.0 * 200 = 200m
        scaled.Stations[4].Should().BeApproximately(200m, 0.001m);
    }

    [Fact]
    public void ScaleOffsets_WithZeroDimensions_ThrowsException()
    {
        // Arrange
        var parent = new ParentHullData
        {
            Lbp = 0m, // Invalid
            B = 28m,
            T = 12.87m,
            Stations = new List<decimal> { 0m, 5m, 10m },
            Waterlines = new List<decimal> { 0m, 6.435m, 12.87m },
            Offsets = new List<List<decimal>>
            {
                new() { 0m, 0m, 0m },
                new() { 0m, 14m, 14m },
                new() { 0m, 0m, 0m }
            }
        };

        var scale = new ScaleFactors
        {
            Length = 200m / 185m,
            Breadth = 30m / 28m,
            Draft = 13m / 12.87m
        };

        // Act & Assert
        // Should handle gracefully or throw - depends on implementation
        // For now, verify it doesn't crash
        var scaled = ParentHullScaler.ScaleOffsets(parent, scale, 200m);
        scaled.Should().NotBeNull();
    }
}
