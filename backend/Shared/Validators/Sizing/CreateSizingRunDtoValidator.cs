using FluentValidation;
using Shared.DTOs.Sizing;

namespace Shared.Validators.Sizing;

public class CreateSizingRunDtoValidator : AbstractValidator<CreateSizingRunDto>
{
    public CreateSizingRunDtoValidator()
    {
        RuleFor(x => x.MissionCaseId)
            .NotEmpty().WithMessage("Mission case ID is required");

        RuleFor(x => x.Mode)
            .NotEmpty()
            .Must(BeValidMode).WithMessage("Mode must be 'first_principles' or 'data_driven'");

        When(x => x.Options != null, () =>
        {
            RuleFor(x => x.Options!.MaxCandidates)
                .GreaterThan(0)
                .LessThanOrEqualTo(20).WithMessage("Max candidates must be between 1 and 20");
        });
    }

    private bool BeValidMode(string mode)
    {
        return mode == "first_principles" || mode == "data_driven";
    }
}

