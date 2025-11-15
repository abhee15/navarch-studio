namespace Shared.HullGenerators.Models;

/// <summary>
/// Principal hull dimensions for parametric generation
/// </summary>
public record HullDimensions(
    /// <summary>
    /// Length between perpendiculars (m)
    /// </summary>
    decimal Length,

    /// <summary>
    /// Maximum beam (m)
    /// </summary>
    decimal Beam,

    /// <summary>
    /// Design draft (m)
    /// </summary>
    decimal Draft,

    /// <summary>
    /// Longitudinal center of buoyancy as percentage of Lpp from aft perpendicular
    /// Positive = forward of amidships, Negative = aft of amidships
    /// </summary>
    decimal LcbPercent
);
