using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.HullGenerators.Models;
using Shared.HullGenerators.ParentHull;

namespace Shared.HullGenerators.VesselTypeSpecific;

/// <summary>
/// Product Carrier specific hull generator
/// Inherits from ParentHullHullGenerator with type-specific defaults and overrides
/// </summary>
public class ProductCarrierHullGenerator : ParentHullHullGenerator
{
    public ProductCarrierHullGenerator(ILogger<ProductCarrierHullGenerator>? logger = null)
        : base(logger, "product_carrier") // Set default vessel type
    {
    }

    // Note: Generate method is inherited from ParentHullHullGenerator
    // The vessel type is set in the constructor, so the base class will use it automatically

    /// <summary>
    /// Get default LCB percent for Product Carrier
    /// Typically 1.5% to 2.5% forward
    /// </summary>
    public static decimal GetDefaultLCBPercent(decimal cb)
    {
        // Product Carriers typically have LCB forward
        // Higher Cb = more forward LCB
        return 1.5m + 0.5m * (cb - 0.75m); // Range: 1.5% to 2.5%
    }

    /// <summary>
    /// Validate Product Carrier parameter ranges
    /// </summary>
    public static bool ValidateParameters(
        decimal length,
        decimal beam,
        decimal draft,
        decimal cb)
    {
        // Check L/B ratio
        decimal l_b = length / beam;
        if (l_b < BSRAConstants.VesselTypeRanges.ProductCarrier.L_B_Min ||
            l_b > BSRAConstants.VesselTypeRanges.ProductCarrier.L_B_Max)
            return false;

        // Check Cb range
        if (cb < BSRAConstants.VesselTypeRanges.ProductCarrier.Cb_Min ||
            cb > BSRAConstants.VesselTypeRanges.ProductCarrier.Cb_Max)
            return false;

        return true;
    }
}
