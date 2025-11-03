using FluentValidation;
using Shared.DTOs.Sizing;

namespace Shared.Validators.Sizing;

public class CreateMissionCaseDtoValidator : AbstractValidator<CreateMissionCaseDto>
{
    public CreateMissionCaseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mission case name is required")
            .MaximumLength(200);

        RuleFor(x => x.MissionType)
            .NotEmpty()
            .Must(BeValidMissionType).WithMessage("Must be: commercial, government, pleasure, research, military");

        RuleFor(x => x.CargoBasis)
            .NotEmpty()
            .Must(BeValidCargoBasis).WithMessage("Must be: volume, weight, teu");

        RuleFor(x => x.CargoValue)
            .GreaterThan(0);

        When(x => x.CargoBasis == "volume", () =>
        {
            RuleFor(x => x.CargoDensityTPerM3).NotNull().GreaterThan(0);
            RuleFor(x => x.CargoVolumeM3).NotNull().GreaterThan(0);
        });

        When(x => x.CargoBasis == "teu", () =>
        {
            RuleFor(x => x.TeuCount).NotNull().GreaterThan(0);
        });

        RuleFor(x => x.ServiceSpeedKn)
            .GreaterThan(0)
            .LessThan(50).WithMessage("Speed must be less than 50 knots");

        RuleFor(x => x.SeaMarginPct)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(50);
    }

    private bool BeValidMissionType(string type)
    {
        var valid = new[] { "commercial", "government", "pleasure", "research", "military" };
        return valid.Contains(type.ToLower());
    }

    private bool BeValidCargoBasis(string basis)
    {
        var valid = new[] { "volume", "weight", "teu" };
        return valid.Contains(basis.ToLower());
    }
}

public class UpdateMissionCaseDtoValidator : AbstractValidator<UpdateMissionCaseDto>
{
    public UpdateMissionCaseDtoValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(200);
        });

        When(x => x.MissionType != null, () =>
        {
            RuleFor(x => x.MissionType!).Must(BeValidMissionType);
        });

        When(x => x.CargoBasis != null, () =>
        {
            RuleFor(x => x.CargoBasis!).Must(BeValidCargoBasis);
        });

        When(x => x.CargoValue.HasValue, () =>
        {
            RuleFor(x => x.CargoValue!.Value).GreaterThan(0);
        });

        When(x => x.ServiceSpeedKn.HasValue, () =>
        {
            RuleFor(x => x.ServiceSpeedKn!.Value).GreaterThan(0).LessThan(50);
        });
    }

    private bool BeValidMissionType(string type)
    {
        var valid = new[] { "commercial", "government", "pleasure", "research", "military" };
        return valid.Contains(type.ToLower());
    }

    private bool BeValidCargoBasis(string basis)
    {
        var valid = new[] { "volume", "weight", "teu" };
        return valid.Contains(basis.ToLower());
    }
}
