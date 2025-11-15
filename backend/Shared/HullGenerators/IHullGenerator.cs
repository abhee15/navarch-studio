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
    /// <returns>Generated hull geometry (stations, waterlines, offsets)</returns>
    GeneratedHullGeometry Generate(
        HullDimensions dims,
        decimal cb,
        decimal cp,
        decimal cm,
        decimal cwp,
        int numStations = 23,
        int numWaterlines = 13);
}
