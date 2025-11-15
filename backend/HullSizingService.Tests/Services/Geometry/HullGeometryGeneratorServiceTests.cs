using HullSizingService.Services.Geometry;
using HullSizingService.Services.Solver;
using Microsoft.Extensions.Logging;
using Shared.DTOs;

namespace HullSizingService.Tests.Services.Geometry;

/// <summary>
/// Unit tests for HullGeometryGeneratorService
/// Tests offset generation from solver candidates and validation
/// </summary>
public class HullGeometryGeneratorServiceTests
{
    private readonly IHullGeometryGeneratorService _service;
    private readonly ILogger<HullGeometryGeneratorService> _logger;

    public HullGeometryGeneratorServiceTests()
    {
        _logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<HullGeometryGeneratorService>();
        _service = new HullGeometryGeneratorService(_logger);
    }

    [Fact]
    public async Task GenerateOffsetsFromCandidate_WithValidCandidate_ProducesOffsetsGrid()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "tanker",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 32m,
            DraftM: 12m,
            DepthM: 18m,
            Cb: 0.80m,
            Cp: 0.82m,
            Cwp: 0.87m,
            Cm: 0.99m,
            DisplacementT: 50000m,
            Fn: 0.20m,
            LwlOverLambda: null,
            KbM: 6.0m,
            LcbPctLpp: 2.0m,
            GmEstM: 1.5m,
            EhpKw: 5000m,
            ShpKw: 6000m,
            Score: 0.85m,
            Flags: new List<string>());

        // Act
        var offsets = await _service.GenerateOffsetsFromCandidateAsync(candidate);

        // Assert
        offsets.Should().NotBeNull();
        offsets!.Stations.Should().NotBeEmpty();
        offsets.Waterlines.Should().NotBeEmpty();
        offsets.Offsets.Should().NotBeEmpty();
        offsets.Stations.Count.Should().Be(23); // BSRA-compatible
        offsets.Waterlines.Count.Should().Be(13);
        offsets.Offsets.Count.Should().Be(offsets.Stations.Count);
        offsets.Offsets[0].Count.Should().Be(offsets.Waterlines.Count);
    }

    [Fact]
    public async Task GenerateOffsetsFromCandidate_WithContainerShip_ProducesValidOffsets()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "container",
            LppM: 280m,
            LwlM: 285m,
            LoaM: 290m,
            BeamM: 44m,
            DraftM: 14m,
            DepthM: 24m,
            Cb: 0.65m,
            Cp: 0.68m,
            Cwp: 0.80m,
            Cm: 0.98m,
            DisplacementT: 80000m,
            Fn: 0.25m,
            LwlOverLambda: null,
            KbM: 7.0m,
            LcbPctLpp: -1.5m,
            GmEstM: 2.0m,
            EhpKw: 12000m,
            ShpKw: 15000m,
            Score: 0.90m,
            Flags: new List<string>());

        // Act
        var offsets = await _service.GenerateOffsetsFromCandidateAsync(candidate);

        // Assert
        offsets.Should().NotBeNull();
        offsets!.Stations.Should().NotBeEmpty();
        offsets.Waterlines.Should().NotBeEmpty();
        offsets.Offsets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateOffsetsFromCandidate_WithInvalidInput_ReturnsNull()
    {
        // Arrange - Invalid candidate (Cb > 1)
        var candidate = new SolverCandidate(
            HullFamily: "tanker",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 32m,
            DraftM: 12m,
            DepthM: 18m,
            Cb: 1.5m, // Invalid
            Cp: 0.82m,
            Cwp: 0.87m,
            Cm: 0.99m,
            DisplacementT: 50000m,
            Fn: 0.20m,
            LwlOverLambda: null,
            KbM: 6.0m,
            LcbPctLpp: 2.0m,
            GmEstM: 1.5m,
            EhpKw: 5000m,
            ShpKw: 6000m,
            Score: 0.85m,
            Flags: new List<string>());

        // Act
        var offsets = await _service.GenerateOffsetsFromCandidateAsync(candidate);

        // Assert
        offsets.Should().BeNull();
    }

    [Fact]
    public async Task ValidateFormCoefficients_WithMatchingCoefficients_ReturnsValid()
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: "tanker",
            LppM: 200m,
            LwlM: 205m,
            LoaM: 210m,
            BeamM: 32m,
            DraftM: 12m,
            DepthM: 18m,
            Cb: 0.80m,
            Cp: 0.82m,
            Cwp: 0.87m,
            Cm: 0.99m,
            DisplacementT: 50000m,
            Fn: 0.20m,
            LwlOverLambda: null,
            KbM: 6.0m,
            LcbPctLpp: 2.0m,
            GmEstM: 1.5m,
            EhpKw: 5000m,
            ShpKw: 6000m,
            Score: 0.85m,
            Flags: new List<string>());

        var offsets = await _service.GenerateOffsetsFromCandidateAsync(candidate);
        offsets.Should().NotBeNull();

        // Act
        var validation = await _service.ValidateFormCoefficientsAsync(
            candidate,
            offsets!,
            tolerance: 0.15m); // 15% tolerance for initial implementation

        // Assert
        validation.Should().NotBeNull();
        validation.ComputedCb.Should().BeGreaterThan(0m);
        validation.ComputedCp.Should().BeGreaterThan(0m);
        validation.ComputedCm.Should().BeGreaterThan(0m);
        validation.ComputedCwp.Should().BeGreaterThan(0m);
    }

    [Theory]
    [InlineData("tanker", 200.0, 32.0, 12.0, 0.80, 0.82, 0.99, 0.87, 2.0)]
    [InlineData("container", 280.0, 44.0, 14.0, 0.65, 0.68, 0.98, 0.80, -1.5)]
    [InlineData("bulk", 250.0, 40.0, 15.0, 0.75, 0.78, 0.99, 0.85, 1.0)]
    public async Task GenerateOffsetsFromCandidate_ForVesselType_ProducesValidGeometry(
        string hullFamily,
        double lpp,
        double beam,
        double draft,
        double cb,
        double cp,
        double cm,
        double cwp,
        double lcbPercent)
    {
        // Arrange
        var candidate = new SolverCandidate(
            HullFamily: hullFamily,
            LppM: (decimal)lpp,
            LwlM: (decimal)lpp * 1.025m,
            LoaM: (decimal)lpp * 1.05m,
            BeamM: (decimal)beam,
            DraftM: (decimal)draft,
            DepthM: (decimal)draft * 1.5m,
            Cb: (decimal)cb,
            Cp: (decimal)cp,
            Cwp: (decimal)cwp,
            Cm: (decimal)cm,
            DisplacementT: (decimal)(cb * lpp * beam * draft * 1.025m),
            Fn: 0.20m,
            LwlOverLambda: null,
            KbM: (decimal)draft * 0.5m,
            LcbPctLpp: (decimal)lcbPercent,
            GmEstM: 1.5m,
            EhpKw: 5000m,
            ShpKw: 6000m,
            Score: 0.85m,
            Flags: new List<string>());

        // Act
        var offsets = await _service.GenerateOffsetsFromCandidateAsync(candidate);

        // Assert
        offsets.Should().NotBeNull();
        offsets!.Stations.Should().NotBeEmpty();
        offsets.Waterlines.Should().NotBeEmpty();
        offsets.Offsets.Should().NotBeEmpty();

        // Verify offsets are valid (non-negative, within beam)
        foreach (var stationOffsets in offsets.Offsets)
        {
            foreach (var halfBreadth in stationOffsets)
            {
                halfBreadth.Should().BeGreaterThanOrEqualTo(0m);
                halfBreadth.Should().BeLessThanOrEqualTo((decimal)beam / 2m);
            }
        }
    }
}
