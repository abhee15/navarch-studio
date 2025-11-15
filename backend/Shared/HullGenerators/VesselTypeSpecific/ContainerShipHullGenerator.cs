using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.HullGenerators.ParentHull;

namespace Shared.HullGenerators.VesselTypeSpecific;

/// <summary>
/// Container Ship specific hull generator
/// </summary>
public class ContainerShipHullGenerator : ParentHullHullGenerator
{
    public ContainerShipHullGenerator(ILogger<ContainerShipHullGenerator>? logger = null)
        : base(logger, "container")
    {
    }

    /// <summary>
    /// Get default LCB percent for Container Ship
    /// Typically 3.0% to 4.0% forward (fine ends)
    /// </summary>
    public static decimal GetDefaultLCBPercent(decimal cb)
    {
        // Container ships have fine ends, LCB more forward
        // Lower Cb = more forward LCB
        return 3.0m + 1.0m * (0.65m - cb); // Range: 3.0% to 4.0%
    }

    /// <summary>
    /// Validate Container Ship parameter ranges
    /// </summary>
    public static bool ValidateParameters(
        decimal length,
        decimal beam,
        decimal draft,
        decimal cb)
    {
        // Check L/B ratio
        decimal l_b = length / beam;
        if (l_b < BSRAConstants.VesselTypeRanges.Container.L_B_Min ||
            l_b > BSRAConstants.VesselTypeRanges.Container.L_B_Max)
            return false;

        // Check Cb range
        if (cb < BSRAConstants.VesselTypeRanges.Container.Cb_Min ||
            cb > BSRAConstants.VesselTypeRanges.Container.Cb_Max)
            return false;

        return true;
    }
}
