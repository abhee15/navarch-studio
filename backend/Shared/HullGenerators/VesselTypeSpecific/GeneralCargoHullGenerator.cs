using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.HullGenerators.ParentHull;

namespace Shared.HullGenerators.VesselTypeSpecific;

/// <summary>
/// General Cargo specific hull generator
/// </summary>
public class GeneralCargoHullGenerator : ParentHullHullGenerator
{
    public GeneralCargoHullGenerator(ILogger<GeneralCargoHullGenerator>? logger = null)
        : base(logger, "general_cargo")
    {
    }

    /// <summary>
    /// Get default LCB percent for General Cargo
    /// Typically 2.0% to 2.8% forward
    /// </summary>
    public static decimal GetDefaultLCBPercent(decimal cb)
    {
        // General cargo has moderate form, LCB moderately forward
        return 2.0m + 0.8m * (0.72m - cb); // Range: 2.0% to 2.8%
    }

    /// <summary>
    /// Validate General Cargo parameter ranges
    /// </summary>
    public static bool ValidateParameters(
        decimal length,
        decimal beam,
        decimal draft,
        decimal cb)
    {
        // Check L/B ratio
        decimal l_b = length / beam;
        if (l_b < BSRAConstants.VesselTypeRanges.GeneralCargo.L_B_Min ||
            l_b > BSRAConstants.VesselTypeRanges.GeneralCargo.L_B_Max)
            return false;

        // Check Cb range
        if (cb < BSRAConstants.VesselTypeRanges.GeneralCargo.Cb_Min ||
            cb > BSRAConstants.VesselTypeRanges.GeneralCargo.Cb_Max)
            return false;

        return true;
    }
}
