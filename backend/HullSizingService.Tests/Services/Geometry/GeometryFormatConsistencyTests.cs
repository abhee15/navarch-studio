using HullSizingService.Data;
using HullSizingService.Services;
using HullSizingService.Services.Geometry;
using HullSizingService.Services.Solver;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs;
using Shared.HullGenerators;
using Xunit;

namespace HullSizingService.Tests.Services.Geometry;

/// <summary>
/// Tests for geometry format consistency across generation methods
/// Ensures all candidates store OffsetsGrid format
/// </summary>
public class GeometryFormatConsistencyTests : IDisposable
{
    private readonly Mock<ILogger<HullGeometryGeneratorService>> _geometryLoggerMock;
    private readonly HullGeometryGeneratorService _geometryGenerator;

    public GeometryFormatConsistencyTests()
    {
        _geometryLoggerMock = new Mock<ILogger<HullGeometryGeneratorService>>();
        _geometryGenerator = new HullGeometryGeneratorService(_geometryLoggerMock.Object);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    [Fact]
    public async Task GeneratedGeometry_ShouldBeOffsetsGridFormat()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "container",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 30m,
            DraftM: 12m,
            DepthM: 15m,
            Cb: 0.68m,
            Cp: 0.73m,
            Cwp: 0.80m,
            Cm: 0.93m,
            DisplacementT: 50000m,
            Fn: 0.25m,
            LwlOverLambda: null,
            KbM: null,
            LcbPctLpp: 0.5m,
            GmEstM: null,
            EhpKw: null,
            ShpKw: null,
            Score: 85m,
            Flags: new List<string>()
        );

        // Act
        var offsetsGrid = await _geometryGenerator.GenerateOffsetsFromCandidateAsync(
            candidate,
            vesselType: "container",
            numStations: 23,
            numWaterlines: 13,
            bowFamily: null,
            midshipFamily: null,
            sternFamily: null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(offsetsGrid);
        Assert.NotNull(offsetsGrid!.Stations);
        Assert.NotNull(offsetsGrid.Waterlines);
        Assert.NotNull(offsetsGrid.Offsets);

        // Verify OffsetsGrid structure
        Assert.Equal(23, offsetsGrid.Stations.Count);
        Assert.Equal(13, offsetsGrid.Waterlines.Count);
        Assert.Equal(23, offsetsGrid.Offsets.Count);
        Assert.All(offsetsGrid.Offsets, stationOffsets => Assert.Equal(13, stationOffsets.Count));
    }

    [Fact]
    public async Task GeneratedGeometry_ShouldHaveBSRAStationLayout()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "container",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 30m,
            DraftM: 12m,
            DepthM: 15m,
            Cb: 0.68m,
            Cp: 0.73m,
            Cwp: 0.80m,
            Cm: 0.93m,
            DisplacementT: 50000m,
            Fn: 0.25m,
            LwlOverLambda: null,
            KbM: null,
            LcbPctLpp: 0.5m,
            GmEstM: null,
            EhpKw: null,
            ShpKw: null,
            Score: 85m,
            Flags: new List<string>()
        );

        // Act
        var offsetsGrid = await _geometryGenerator.GenerateOffsetsFromCandidateAsync(
            candidate,
            vesselType: "container",
            numStations: 23,
            numWaterlines: 13,
            bowFamily: null,
            midshipFamily: null,
            sternFamily: null,
            CancellationToken.None);

        // Assert - Verify BSRA station layout (0, 0.25, 0.5, ..., 10.0 normalized)
        Assert.NotNull(offsetsGrid);
        Assert.Equal(23, offsetsGrid!.Stations.Count);

        // First station should be at 0 (AP)
        Assert.Equal(0m, offsetsGrid.Stations[0]);

        // Last station should be at length (FP)
        Assert.Equal(200m, offsetsGrid.Stations[^1]);

        // Midship should be approximately at Lpp/2
        var midshipIdx = offsetsGrid.Stations.Count / 2;
        Assert.True(Math.Abs(offsetsGrid.Stations[midshipIdx] - 100m) < 1m);
    }

    [Fact]
    public async Task GeneratedGeometry_ShouldHaveValidOffsets()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "container",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 30m,
            DraftM: 12m,
            DepthM: 15m,
            Cb: 0.68m,
            Cp: 0.73m,
            Cwp: 0.80m,
            Cm: 0.93m,
            DisplacementT: 50000m,
            Fn: 0.25m,
            LwlOverLambda: null,
            KbM: null,
            LcbPctLpp: 0.5m,
            GmEstM: null,
            EhpKw: null,
            ShpKw: null,
            Score: 85m,
            Flags: new List<string>()
        );

        // Act
        var offsetsGrid = await _geometryGenerator.GenerateOffsetsFromCandidateAsync(
            candidate,
            vesselType: "container",
            numStations: 23,
            numWaterlines: 13,
            bowFamily: null,
            midshipFamily: null,
            sternFamily: null,
            CancellationToken.None);

        // Assert - All offsets should be non-negative and reasonable
        Assert.NotNull(offsetsGrid);
        foreach (var stationOffsets in offsetsGrid!.Offsets)
        {
            foreach (var offset in stationOffsets)
            {
                Assert.True(offset >= 0m, "Offsets must be non-negative");
                Assert.True(offset <= candidate.BeamM / 2m + 1m, "Offsets should not exceed half beam significantly");
            }
        }
    }

    [Fact]
    public async Task GeneratedGeometry_ShouldBeNonIsometric()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "container",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 30m,
            DraftM: 12m,
            DepthM: 15m,
            Cb: 0.68m,
            Cp: 0.73m,
            Cwp: 0.80m,
            Cm: 0.93m,
            DisplacementT: 50000m,
            Fn: 0.25m,
            LwlOverLambda: null,
            KbM: null,
            LcbPctLpp: 2.0m, // Forward LCB to create asymmetry
            GmEstM: null,
            EhpKw: null,
            ShpKw: null,
            Score: 85m,
            Flags: new List<string>()
        );

        // Act
        var offsetsGrid = await _geometryGenerator.GenerateOffsetsFromCandidateAsync(
            candidate,
            vesselType: "container",
            numStations: 23,
            numWaterlines: 13,
            bowFamily: null,
            midshipFamily: null,
            sternFamily: null,
            CancellationToken.None);

        // Assert - Verify non-isometric (asymmetric) geometry
        Assert.NotNull(offsetsGrid);
        var numStations = offsetsGrid!.Stations.Count;
        var midshipIdx = numStations / 2;
        var tolerance = 0.001m;

        // Compare corresponding stations from bow and stern
        for (int i = 0; i < midshipIdx; i++)
        {
            var sternIdx = i;
            var bowIdx = numStations - 1 - i;

            // Check at design draft waterline (last waterline)
            var designDraftIdx = offsetsGrid.Waterlines.Count - 1;
            var sternOffset = offsetsGrid.Offsets[sternIdx][designDraftIdx];
            var bowOffset = offsetsGrid.Offsets[bowIdx][designDraftIdx];

            // Offsets should be different (not symmetric) due to LCB shift
            // Note: Even with LCB = 0, form coefficients create asymmetry, but LCB != 0 makes it more pronounced
            if (i < 5) // Check first few stations (bow/stern regions)
            {
                var difference = Math.Abs(sternOffset - bowOffset);
                // With forward LCB, bow should generally be fuller than stern
                // But the key is that they're different (non-isometric)
                Assert.True(difference > tolerance || Math.Abs(candidate.LcbPctLpp ?? 0m) < 0.1m,
                    $"Stations {sternIdx} and {bowIdx} should be different (non-isometric). " +
                    $"Stern: {sternOffset}, Bow: {bowOffset}, Difference: {difference}");
            }
        }
    }
}
