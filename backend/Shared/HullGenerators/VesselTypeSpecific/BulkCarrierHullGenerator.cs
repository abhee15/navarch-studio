using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.HullGenerators.ParentHull;

namespace Shared.HullGenerators.VesselTypeSpecific;

/// <summary>
/// Bulk Carrier specific hull generator
/// </summary>
public class BulkCarrierHullGenerator : ParentHullHullGenerator
{
    public BulkCarrierHullGenerator(ILogger<BulkCarrierHullGenerator>? logger = null)
        : base(logger, "bulk_carrier")
    {
    }

    /// <summary>
    /// Get default LCB percent for Bulk Carrier
    /// Typically 1.3% to 1.7% forward
    /// </summary>
    public static decimal GetDefaultLCBPercent(decimal cb)
    {
        // Bulk carriers have very full forms, LCB less forward
        return 1.3m + 0.4m * (cb - 0.80m); // Range: 1.3% to 1.7%
    }

    /// <summary>
    /// Validate Bulk Carrier parameter ranges
    /// </summary>
    public static bool ValidateParameters(
        decimal length,
        decimal beam,
        decimal draft,
        decimal cb)
    {
        // Check L/B ratio
        decimal l_b = length / beam;
        if (l_b < BSRAConstants.VesselTypeRanges.BulkCarrier.L_B_Min ||
            l_b > BSRAConstants.VesselTypeRanges.BulkCarrier.L_B_Max)
            return false;

        // Check Cb range
        if (cb < BSRAConstants.VesselTypeRanges.BulkCarrier.Cb_Min ||
            cb > BSRAConstants.VesselTypeRanges.BulkCarrier.Cb_Max)
            return false;

        return true;
    }
}
