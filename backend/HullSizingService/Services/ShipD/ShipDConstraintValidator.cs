using System.Collections.Generic;

namespace HullSizingService.Services.ShipD;

/// <summary>
/// Performs lightweight validation of ShipD parameterization outputs prior to solver execution.
/// </summary>
public class ShipDConstraintValidator : IShipDConstraintValidator
{
    private readonly ILogger<ShipDConstraintValidator> _logger;

    public ShipDConstraintValidator(ILogger<ShipDConstraintValidator> logger)
    {
        _logger = logger;
    }

    public ShipDConstraintValidationResult Validate(ShipDParameterizationResult result)
    {
        var errors = new List<string>();

        if (result.ParameterVector.Count != 45)
        {
            errors.Add($"ShipD vector must contain exactly 45 parameters (received {result.ParameterVector.Count}).");
        }

        if (string.IsNullOrWhiteSpace(result.BowFamily))
        {
            errors.Add("Bow family is required for ShipD generation.");
        }

        if (string.IsNullOrWhiteSpace(result.MidshipFamily))
        {
            errors.Add("Midship family is required for ShipD generation.");
        }

        if (string.IsNullOrWhiteSpace(result.SternFamily))
        {
            errors.Add("Stern family is required for ShipD generation.");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("[SHIPD_VALIDATION] Validation failed: {Errors}", string.Join("; ", errors));
            return new ShipDConstraintValidationResult(false, errors);
        }

        if (result.Warnings.Count > 0)
        {
            _logger.LogInformation("[SHIPD_VALIDATION] Non-blocking warnings: {Warnings}", string.Join("; ", result.Warnings));
        }

        return new ShipDConstraintValidationResult(true, System.Array.Empty<string>());
    }
}
