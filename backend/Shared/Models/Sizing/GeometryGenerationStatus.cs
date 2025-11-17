namespace Shared.Models.Sizing;

/// <summary>
/// Status of geometry generation for a candidate design
/// </summary>
public enum GeometryGenerationStatus
{
    /// <summary>
    /// Geometry generated successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// ShipD geometry generation failed, but form-coefficient generation may have succeeded
    /// </summary>
    ShipDFailed = 1,

    /// <summary>
    /// Form-coefficient geometry generation failed (ShipD may or may not have been attempted)
    /// </summary>
    FormCoefficientFailed = 2,

    /// <summary>
    /// Both ShipD and form-coefficient geometry generation failed
    /// </summary>
    BothFailed = 3
}
