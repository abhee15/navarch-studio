using System.Collections.Generic;
using FluentValidation;
using Shared.DTOs.Sizing;

namespace Shared.Validators.Sizing;

internal static class MissionCaseValidationHelper
{
    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "commercial",
        "government",
        "recreational",
        "research"
    };

    private static readonly HashSet<string> ValidCargoBasis = new(StringComparer.OrdinalIgnoreCase)
    {
        "volume",
        "weight",
        "teu"
    };

    public static bool IsValidMissionCategory(string category) => ValidCategories.Contains(category);

    public static bool IsValidCargoBasis(string basis) => ValidCargoBasis.Contains(basis);
}

public class CreateMissionCaseDtoValidator : AbstractValidator<CreateMissionCaseDto>
{
    public CreateMissionCaseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mission case name is required")
            .MaximumLength(200);

        RuleFor(x => x.MissionCategory)
            .NotEmpty()
            .Must(MissionCaseValidationHelper.IsValidMissionCategory).WithMessage("Must be one of: commercial, government, recreational, research");

        RuleFor(x => x.MissionType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.BowFamily)
            .NotEmpty().WithMessage("Bow family selection is required");

        RuleFor(x => x.MidshipFamily)
            .NotEmpty().WithMessage("Midship family selection is required");

        RuleFor(x => x.SternFamily)
            .NotEmpty().WithMessage("Stern family selection is required");

        RuleFor(x => x.CargoBasis)
            .NotEmpty()
            .Must(MissionCaseValidationHelper.IsValidCargoBasis).WithMessage("Must be: volume, weight, teu");

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
}

public class UpdateMissionCaseDtoValidator : AbstractValidator<UpdateMissionCaseDto>
{
    public UpdateMissionCaseDtoValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(200);
        });

        When(x => x.MissionCategory != null, () =>
        {
            RuleFor(x => x.MissionCategory!).Must(MissionCaseValidationHelper.IsValidMissionCategory);
        });

        When(x => x.MissionType != null, () =>
        {
            RuleFor(x => x.MissionType!)
                .NotEmpty()
                .MaximumLength(100);
        });

        When(x => x.BowFamily != null, () =>
        {
            RuleFor(x => x.BowFamily!).NotEmpty();
        });

        When(x => x.MidshipFamily != null, () =>
        {
            RuleFor(x => x.MidshipFamily!).NotEmpty();
        });

        When(x => x.SternFamily != null, () =>
        {
            RuleFor(x => x.SternFamily!).NotEmpty();
        });

        When(x => x.CargoBasis != null, () =>
        {
            RuleFor(x => x.CargoBasis!).Must(MissionCaseValidationHelper.IsValidCargoBasis);
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
}
