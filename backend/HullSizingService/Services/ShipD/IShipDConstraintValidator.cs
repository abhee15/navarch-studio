namespace HullSizingService.Services.ShipD;

public interface IShipDConstraintValidator
{
    ShipDConstraintValidationResult Validate(ShipDParameterizationResult result);
}

public record ShipDConstraintValidationResult(bool IsValid, IReadOnlyList<string> Errors);

