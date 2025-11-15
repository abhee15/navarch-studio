using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.HullGenerators.ParentHull;

namespace Shared.HullGenerators.VesselTypeSpecific;

/// <summary>
/// Tanker specific hull generator
/// </summary>
public class TankerHullGenerator : ParentHullHullGenerator
{
    public TankerHullGenerator(ILogger<TankerHullGenerator>? logger = null)
        : base(logger, "tanker")
    {
    }

    /// <summary>
    /// Get default LCB percent for Tanker
    /// Typically 1.8% to 2.2% forward
    /// </summary>
    public static decimal GetDefaultLCBPercent(decimal cb)
    {
        // Tankers have full forms, LCB moderately forward
        return 1.8m + 0.4m * (cb - 0.78m); // Range: 1.8% to 2.2%
    }

    /// <summary>
    /// Validate Tanker parameter ranges
    /// </summary>
    public static bool ValidateParameters(
        decimal length,
        decimal beam,
        decimal draft,
        decimal cb)
    {
        // Check L/B ratio
        decimal l_b = length / beam;
        if (l_b < BSRAConstants.VesselTypeRanges.Tanker.L_B_Min ||
            l_b > BSRAConstants.VesselTypeRanges.Tanker.L_B_Max)
            return false;

        // Check Cb range
        if (cb < BSRAConstants.VesselTypeRanges.Tanker.Cb_Min ||
            cb > BSRAConstants.VesselTypeRanges.Tanker.Cb_Max)
            return false;

        return true;
    }
}
