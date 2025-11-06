using FluentAssertions;
using HullSizingService.Services.DataDriven;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace HullSizingService.Tests.Services.DataDriven;

public class VesselScalingServiceTests
{
    private readonly VesselScalingService _service;
    private readonly Mock<ILogger<VesselScalingService>> _loggerMock;

    public VesselScalingServiceTests()
    {
        _loggerMock = new Mock<ILogger<VesselScalingService>>();
        _service = new VesselScalingService(_loggerMock.Object);
    }

    [Fact]
    public void ScaleToTarget_DoublesDisplacement_ScalesBy1_26()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = reference.DisplacementT * 2.0m;  // 104,060t

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement);

        // Assert
        var expectedScale = (decimal)Math.Pow(2.0, 1.0 / 3.0);  // ≈ 1.26
        result.ScaleFactor.Should().BeApproximately(expectedScale, 0.01m);
        result.Lpp.Should().BeApproximately(230.0m * expectedScale, 1.0m);
        result.Beam.Should().BeApproximately(32.2m * expectedScale, 0.5m);
        result.Draft.Should().BeApproximately(10.8m * expectedScale, 0.5m);
    }

    [Fact]
    public void ScaleToTarget_PreservesFormCoefficients()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = 75000.0m;

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement);

        // Assert
        result.Cb.Should().Be(reference.Cb);  // Block coefficient preserved
        result.Cp.Should().Be(reference.Cp);  // Prismatic coefficient preserved
        result.Cm.Should().Be(reference.Cm);  // Midship coefficient preserved
    }

    [Fact]
    public void ScaleToTarget_EstimatesMissingCoefficients()
    {
        // Arrange
        var reference = CreateKCS();
        reference.Cp = null;  // Missing CP
        reference.Cm = null;  // Missing CM
        var targetDisplacement = 75000.0m;

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement);

        // Assert
        result.Cp.Should().NotBeNull();
        result.Cp.Should().BeGreaterThan(0.5m);
        result.Cp.Should().BeLessThanOrEqualTo(1.0m);
        
        result.Cm.Should().NotBeNull();
        result.Cm.Should().BeGreaterThan(0.7m);
        result.Cm.Should().BeLessThanOrEqualTo(1.0m);
    }

    [Fact]
    public void ScaleToTarget_WithBeamConstraint_ClampsAndCompensates()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = reference.DisplacementT * 2.0m;  // Would scale beam to ~40.6m
        var constraints = new ScalingConstraints
        {
            MaxBeam = 35.0m  // Tighter than natural scaling
        };

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement, constraints);

        // Assert
        result.Beam.Should().BeLessThanOrEqualTo(35.0m);  // Constrained
        result.ConstraintsApplied.Should().BeTrue();
        // L and T should be compensated to maintain displacement
        result.Lpp.Should().BeGreaterThan(230.0m * 1.26m);  // Increased more to compensate
    }

    [Fact]
    public void ScaleToTarget_WithDraftConstraint_ClampsAndCompensates()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = reference.DisplacementT * 2.0m;
        var constraints = new ScalingConstraints
        {
            MaxDraft = 12.0m  // Limit draft
        };

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement, constraints);

        // Assert
        result.Draft.Should().BeLessThanOrEqualTo(12.0m);  // Constrained
        result.ConstraintsApplied.Should().BeTrue();
        result.Lpp.Should().BeGreaterThan(230.0m * 1.26m);  // Compensated
        result.Beam.Should().BeGreaterThan(32.2m * 1.26m);  // Compensated
    }

    [Fact]
    public void ScaleToTarget_ExcessiveDistortion_MarksInvalid()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = reference.DisplacementT * 10.0m;  // Huge scale
        var constraints = new ScalingConstraints
        {
            MaxBeam = 35.0m,  // Very tight - will cause major distortion
            MaxDraft = 12.0m
        };

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement, constraints);

        // Assert
        result.IsValid.Should().BeFalse();  // Too much distortion
        result.Distortion.Should().BeGreaterThan(0.10m);  // >10% distortion
    }

    [Fact]
    public void ScaleToTarget_NoConstraints_ProducesValidResult()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = 75000.0m;

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Distortion.Should().BeLessThan(0.01m);  // Minimal distortion without constraints
        result.ConstraintsApplied.Should().BeFalse();
    }

    [Fact]
    public void ScaleToTarget_SmallVessel_ScalesDown()
    {
        // Arrange
        var reference = CreateKCS();
        var targetDisplacement = reference.DisplacementT / 2.0m;  // Half size

        // Act
        var result = _service.ScaleToTarget(reference, targetDisplacement);

        // Assert
        var expectedScale = (decimal)Math.Pow(0.5, 1.0 / 3.0);  // ≈ 0.794
        result.ScaleFactor.Should().BeApproximately(expectedScale, 0.01m);
        result.Lpp.Should().BeLessThan(reference.LppM);
        result.Beam.Should().BeLessThan(reference.BeamM);
        result.Draft.Should().BeLessThan(reference.DraftM);
    }

    private CatalogVesselReal CreateKCS()
    {
        return new CatalogVesselReal
        {
            Id = Guid.NewGuid(),
            VesselId = "KCS",
            VesselType = "Container",
            LppM = 230.0m,
            BeamM = 32.2m,
            DraftM = 10.8m,
            DepthM = 19.0m,
            DisplacementT = 52030.0m,
            Cb = 0.6505m,
            Cp = 0.66m,
            Cm = 0.9849m,
            Cw = 0.83m,
            ServiceSpeedMs = 12.34m,
            IsSystemData = true
        };
    }
}

