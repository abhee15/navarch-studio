using FluentValidation;
using Shared.DTOs.Sizing;

namespace Shared.Validators.Sizing;

/// <summary>
/// Validator for CreateMissionCaseDto
/// </summary>
public class CreateMissionCaseDtoValidator : AbstractValidator<CreateMissionCaseDto>
{
    public CreateMissionCaseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mission case name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.MissionType)
            .NotEmpty().WithMessage("Mission type is required")
            .Must(BeValidMissionType).WithMessage("Mission type must be one of: commercial, government, pleasure, research, military");

        RuleFor(x => x.CargoBasis)
            .NotEmpty().WithMessage("Cargo basis is required")
            .Must(BeValidCargoBasis).WithMessage("Cargo basis must be one of: volume, weight, teu");

        RuleFor(x => x.CargoValue)
            .GreaterThan(0).WithMessage("Cargo value must be positive");

        // Conditional validation based on CargoBasis
        RuleFor(x => x.CargoDensityTPerM3)
            .NotNull().WithMessage("Cargo density is required when cargo basis is 'volume'")
            .GreaterThan(0).WithMessage("Cargo density must be positive")
            .When(x => x.CargoBasis == "volume");

        RuleFor(x => x.CargoVolumeM3)
            .NotNull().WithMessage("Cargo volume is required when cargo basis is 'volume'")
            .GreaterThan(0).WithMessage("Cargo volume must be positive")
            .When(x => x.CargoBasis == "volume");

        RuleFor(x => x.TeuCount)
            .NotNull().WithMessage("TEU count is required when cargo basis is 'teu'")
            .GreaterThan(0).WithMessage("TEU count must be positive")
            .When(x => x.CargoBasis == "teu");

        RuleFor(x => x.ServiceSpeedKn)
            .GreaterThan(0).WithMessage("Service speed must be positive")
            .LessThan(50).WithMessage("Service speed must be less than 50 knots (unrealistic for cargo vessels)");

        RuleFor(x => x.SeaMarginPct)
            .GreaterThanOrEqualTo(0).WithMessage("Sea margin must be non-negative")
            .LessThanOrEqualTo(50).WithMessage("Sea margin must not exceed 50%");

        // Optional constraints - but if provided, must be positive
        When(x => x.EnvHsM.HasValue, () =>
        {
            RuleFor(x => x.EnvHsM!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Significant wave height must be non-negative");
        });

        When(x => x.EnvTzS.HasValue, () =>
        {
            RuleFor(x => x.EnvTzS!.Value)
                .GreaterThan(0).WithMessage("Zero-crossing period must be positive");
        });

        When(x => x.CapLoaM.HasValue, () =>
        {
            RuleFor(x => x.CapLoaM!.Value)
                .GreaterThan(0).WithMessage("LOA constraint must be positive");
        });

        When(x => x.CapBeamM.HasValue, () =>
        {
            RuleFor(x => x.CapBeamM!.Value)
                .GreaterThan(0).WithMessage("Beam constraint must be positive");
        });

        When(x => x.CapDraftM.HasValue, () =>
        {
            RuleFor(x => x.CapDraftM!.Value)
                .GreaterThan(0).WithMessage("Draft constraint must be positive");
        });

        When(x => x.CapAirdraftM.HasValue, () =>
        {
            RuleFor(x => x.CapAirdraftM!.Value)
                .GreaterThan(0).WithMessage("Air draft constraint must be positive");
        });

        When(x => x.EnduranceNm.HasValue, () =>
        {
            RuleFor(x => x.EnduranceNm!.Value)
                .GreaterThan(0).WithMessage("Endurance must be positive");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
    }

    private bool BeValidMissionType(string missionType)
    {
        var validTypes = new[] { "commercial", "government", "pleasure", "research", "military" };
        return validTypes.Contains(missionType.ToLower());
    }

    private bool BeValidCargoBasis(string cargoBasis)
    {
        var validBases = new[] { "volume", "weight", "teu" };
        return validBases.Contains(cargoBasis.ToLower());
    }
}

/// <summary>
/// Validator for UpdateMissionCaseDto
/// </summary>
public class UpdateMissionCaseDtoValidator : AbstractValidator<UpdateMissionCaseDto>
{
    public UpdateMissionCaseDtoValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("Name cannot be empty")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
        });

        When(x => x.MissionType != null, () =>
        {
            RuleFor(x => x.MissionType!)
                .NotEmpty().WithMessage("Mission type cannot be empty")
                .Must(BeValidMissionType).WithMessage("Mission type must be one of: commercial, government, pleasure, research, military");
        });

        When(x => x.CargoBasis != null, () =>
        {
            RuleFor(x => x.CargoBasis!)
                .NotEmpty().WithMessage("Cargo basis cannot be empty")
                .Must(BeValidCargoBasis).WithMessage("Cargo basis must be one of: volume, weight, teu");
        });

        When(x => x.CargoValue.HasValue, () =>
        {
            RuleFor(x => x.CargoValue!.Value)
                .GreaterThan(0).WithMessage("Cargo value must be positive");
        });

        When(x => x.CargoDensityTPerM3.HasValue, () =>
        {
            RuleFor(x => x.CargoDensityTPerM3!.Value)
                .GreaterThan(0).WithMessage("Cargo density must be positive");
        });

        When(x => x.CargoVolumeM3.HasValue, () =>
        {
            RuleFor(x => x.CargoVolumeM3!.Value)
                .GreaterThan(0).WithMessage("Cargo volume must be positive");
        });

        When(x => x.TeuCount.HasValue, () =>
        {
            RuleFor(x => x.TeuCount!.Value)
                .GreaterThan(0).WithMessage("TEU count must be positive");
        });

        When(x => x.ServiceSpeedKn.HasValue, () =>
        {
            RuleFor(x => x.ServiceSpeedKn!.Value)
                .GreaterThan(0).WithMessage("Service speed must be positive")
                .LessThan(50).WithMessage("Service speed must be less than 50 knots");
        });

        When(x => x.SeaMarginPct.HasValue, () =>
        {
            RuleFor(x => x.SeaMarginPct!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Sea margin must be non-negative")
                .LessThanOrEqualTo(50).WithMessage("Sea margin must not exceed 50%");
        });

        When(x => x.Notes != null, () =>
        {
            RuleFor(x => x.Notes!)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
        });
    }

    private bool BeValidMissionType(string missionType)
    {
        var validTypes = new[] { "commercial", "government", "pleasure", "research", "military" };
        return validTypes.Contains(missionType.ToLower());
    }

    private bool BeValidCargoBasis(string cargoBasis)
    {
        var validBases = new[] { "volume", "weight", "teu" };
        return validBases.Contains(cargoBasis.ToLower());
    }
}

