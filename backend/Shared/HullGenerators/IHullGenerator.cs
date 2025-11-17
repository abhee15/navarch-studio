using Shared.HullGenerators.Models;

namespace Shared.HullGenerators;

/// <summary>
/// Interface for parametric hull form generators
/// Generates hull offsets from form coefficients and principal dimensions
/// </summary>
public interface IHullGenerator
{
    /// <summary>
    /// Generate hull offsets from form coefficients
    /// </summary>
    /// <param name="dims">Principal dimensions (L, B, T, LCB%)</param>
    /// <param name="cb">Block coefficient</param>
    /// <param name="cp">Prismatic coefficient</param>
    /// <param name="cm">Midship coefficient</param>
    /// <param name="cwp">Waterplane coefficient</param>
    /// <param name="numStations">Number of stations (default: 23 for BSRA-compatible)</param>
    /// <param name="numWaterlines">Number of waterlines (default: 13)</param>
    /// <param name="bowFamily">Optional ShipD bow family (e.g., "bulbous_bow", "axe_bow", "fine_entry") to influence shape</param>
    /// <param name="midshipFamily">Optional ShipD midship family (e.g., "full_midship", "fine_midship") to influence shape</param>
    /// <param name="sternFamily">Optional ShipD stern family (e.g., "transom_stern", "twin_skeg", "fine_stern") to influence shape</param>
    /// <param name="vesselType">Optional vessel type (e.g., "yacht", "cargo", "container") for additional shape adjustments</param>
    /// <returns>Generated hull geometry (stations, waterlines, offsets)</returns>
    GeneratedHullGeometry Generate(
        HullDimensions dims,
        decimal cb,
        decimal cp,
        decimal cm,
        decimal cwp,
        int numStations = 23,
        int numWaterlines = 13,
        string? bowFamily = null,
        string? midshipFamily = null,
        string? sternFamily = null,
        string? vesselType = null);
}
