using Shared.DTOs;

namespace DataService.Services.Resistance;

/// <summary>
/// Implementation of default values service with lookup tables
/// Note: This is a minimal in-code implementation. Can be migrated to Catalog later.
/// </summary>
public class DefaultValuesService : IDefaultValuesService
{
    private readonly ILogger<DefaultValuesService> _logger;

    public DefaultValuesService(ILogger<DefaultValuesService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets default values based on vessel characteristics
    /// Uses vessel type and form coefficients to determine appropriate defaults
    /// </summary>
    public DefaultValuesResponseDto GetDefaultValues(DefaultValuesRequestDto request)
    {
        _logger.LogInformation(
            "Computing default values for VesselType={VesselType}, CB={CB}, LB={LB}, BT={BT}",
            request.VesselType, request.CB, request.LB_Ratio, request.BT_Ratio);

        var response = new DefaultValuesResponseDto();

        // Determine vessel category based on type or coefficients
        var category = DetermineVesselCategory(request);

        // Get defaults for each parameter
        response.FormFactor = GetFormFactorDefault(category, request);
        response.AppendageAreaPercent = GetAppendageAreaDefault(category, request);
        response.RoughnessAllowance = GetRoughnessAllowanceDefault(category);
        response.EtaD = GetEtaDDefault(category, request);
        response.EtaH = GetEtaHDefault(category, request);
        response.EtaR = GetEtaRDefault(category);
        response.EtaO = GetEtaODefault(category, request);
        response.CM = GetCMDefault(category, request);

        // Calculate wetted surface area if dimensions provided
        if (request.Lpp.HasValue && request.Beam.HasValue && request.Draft.HasValue)
        {
            response.WettedSurfaceArea = EstimateWettedSurfaceArea(
                request.Lpp.Value, request.Beam.Value, request.Draft.Value,
                response.CM?.Value ?? 0.98m);
        }

        response.Provenance = $"Default values for {category} vessels";

        _logger.LogInformation("Default values computed successfully for category: {Category}", category);

        return response;
    }

    /// <summary>
    /// Determines vessel category from type or characteristics
    /// </summary>
    private string DetermineVesselCategory(DefaultValuesRequestDto request)
    {
        // If explicit type provided, use it
        if (!string.IsNullOrEmpty(request.VesselType))
        {
            return request.VesselType;
        }

        // Otherwise, infer from CB and ratios
        if (request.CB.HasValue)
        {
            var cb = request.CB.Value;

            // Full-bodied vessels (high CB)
            if (cb >= 0.75m)
                return "Tanker";

            // Medium fullness
            if (cb >= 0.65m)
                return "CargoShip";

            // Fine-lined vessels (low CB)
            if (cb >= 0.50m)
                return "ContainerShip";

            // Very fine (fast vessels)
            return "Ferry";
        }

        // Default to general cargo
        return "General";
    }

    /// <summary>
    /// Gets form factor k based on vessel category and CB
    /// </summary>
    private DefaultValueItem GetFormFactorDefault(string category, DefaultValuesRequestDto request)
    {
        // Lookup table: Form factor k by vessel type and CB
        // Reference: Holtrop & Mennen (1982), typical values
        var defaultK = category switch
        {
            "Tanker" => 0.15m,          // Full-bodied, low form factor
            "Bulker" => 0.16m,
            "CargoShip" => 0.18m,       // General cargo
            "ContainerShip" => 0.22m,   // Fine-lined, higher form factor
            "Ferry" => 0.24m,           // Fast vessels
            "Yacht" => 0.20m,           // Recreational craft
            "Tug" => 0.25m,             // Small, robust vessels
            _ => 0.20m                  // General default
        };

        // Adjust based on CB if available
        if (request.CB.HasValue)
        {
            var cb = request.CB.Value;

            // Fine-lined vessels (low CB) tend to have higher k
            if (cb < 0.60m)
                defaultK += 0.03m;
            else if (cb > 0.75m)
                defaultK -= 0.03m;
        }

        return new DefaultValueItem
        {
            Value = defaultK,
            Provenance = $"Default for {category} (CB-adjusted)",
            Range = "Typical range: 0.15-0.30"
        };
    }

    /// <summary>
    /// Gets appendage area percentage based on vessel type
    /// </summary>
    private DefaultValueItem GetAppendageAreaDefault(string category, DefaultValuesRequestDto request)
    {
        // Lookup table: Appendage area as % of wetted surface
        // Includes: rudder, bilge keels, shaft brackets, stabilizers
        var defaultPercent = category switch
        {
            "Tanker" => 2.5m,           // Simple hull, minimal appendages
            "Bulker" => 3.0m,
            "CargoShip" => 4.0m,        // Standard appendages
            "ContainerShip" => 5.0m,    // Larger rudders, stabilizers
            "Ferry" => 6.0m,            // Multiple stabilizers, complex stern
            "Yacht" => 4.5m,            // Keel, rudder, trim tabs
            "Tug" => 7.0m,              // Large propulsion appendages
            _ => 4.0m                   // General default
        };

        // Convert to generic factor: factor = 1 + (percent / 100)
        var genericFactor = 1.0m + (defaultPercent / 100m);

        return new DefaultValueItem
        {
            Value = genericFactor,
            Provenance = $"Typical {category} appendages",
            Range = "Typical range: 2-10%"
        };
    }

    /// <summary>
    /// Gets roughness allowance (correlation allowance CA)
    /// </summary>
    private DefaultValueItem GetRoughnessAllowanceDefault(string category)
    {
        // ITTC-1978 correlation allowance
        // Standard value for well-maintained ships
        var ca = 0.0004m;

        // Older or less maintained vessels might have higher CA
        if (category == "Bulker" || category == "Tanker")
        {
            ca = 0.0005m; // Slightly higher for larger vessels
        }

        return new DefaultValueItem
        {
            Value = ca,
            Provenance = "ITTC-1978 standard",
            Range = "Typical range: 0.0002-0.0006"
        };
    }

    /// <summary>
    /// Gets overall propulsive efficiency ηD
    /// </summary>
    private DefaultValueItem GetEtaDDefault(string category, DefaultValuesRequestDto request)
    {
        // Overall efficiency ηD = ηH × ηR × ηO
        // Typical values from ship design handbooks
        var defaultEtaD = category switch
        {
            "Tanker" => 0.70m,          // Slow, efficient
            "Bulker" => 0.68m,
            "CargoShip" => 0.65m,       // Medium speed
            "ContainerShip" => 0.62m,   // Higher speed, lower efficiency
            "Ferry" => 0.60m,           // Fast, waterjet/multiple props
            "Yacht" => 0.55m,           // Small craft, less efficient
            "Tug" => 0.58m,             // High power, complex flow
            _ => 0.65m                  // General default
        };

        return new DefaultValueItem
        {
            Value = defaultEtaD,
            Provenance = $"Typical for {category}",
            Range = "Typical range: 0.55-0.75"
        };
    }

    /// <summary>
    /// Gets hull efficiency ηH
    /// </summary>
    private DefaultValueItem GetEtaHDefault(string category, DefaultValuesRequestDto request)
    {
        // Hull efficiency: ratio of thrust to tow force
        // Typically slightly above 1.0 (thrust deduction)
        var defaultEtaH = category switch
        {
            "Tanker" => 1.04m,
            "Bulker" => 1.03m,
            "CargoShip" => 1.02m,
            "ContainerShip" => 1.00m,   // Fine stern, less augmentation
            "Ferry" => 0.98m,           // Fast vessels, thrust deduction
            "Yacht" => 0.97m,
            _ => 1.00m
        };

        return new DefaultValueItem
        {
            Value = defaultEtaH,
            Provenance = $"Typical for {category}",
            Range = "Typical range: 0.95-1.05"
        };
    }

    /// <summary>
    /// Gets relative rotative efficiency ηR
    /// </summary>
    private DefaultValueItem GetEtaRDefault(string category)
    {
        // Relative rotative efficiency: accounts for flow rotation
        // Typically close to 1.0
        var defaultEtaR = 1.02m;

        return new DefaultValueItem
        {
            Value = defaultEtaR,
            Provenance = "Standard for single-screw vessels",
            Range = "Typical range: 1.00-1.05"
        };
    }

    /// <summary>
    /// Gets open water efficiency ηO
    /// </summary>
    private DefaultValueItem GetEtaODefault(string category, DefaultValuesRequestDto request)
    {
        // Open water propeller efficiency
        // Depends on propeller design and loading
        var defaultEtaO = category switch
        {
            "Tanker" => 0.68m,          // Low speed, well-optimized
            "Bulker" => 0.67m,
            "CargoShip" => 0.65m,
            "ContainerShip" => 0.62m,   // Higher speed, higher loading
            "Ferry" => 0.60m,           // Fast, multiple props or waterjet
            "Yacht" => 0.55m,           // Small craft
            _ => 0.65m
        };

        return new DefaultValueItem
        {
            Value = defaultEtaO,
            Provenance = $"Typical propeller efficiency for {category}",
            Range = "Typical range: 0.50-0.70"
        };
    }

    /// <summary>
    /// Gets midship coefficient CM
    /// </summary>
    private DefaultValueItem GetCMDefault(string category, DefaultValuesRequestDto request)
    {
        // Midship coefficient: ratio of midship area to rectangle (B × T)
        var defaultCM = category switch
        {
            "Tanker" => 0.99m,          // Very full midship
            "Bulker" => 0.98m,
            "CargoShip" => 0.97m,
            "ContainerShip" => 0.95m,   // More refined
            "Ferry" => 0.93m,
            "Yacht" => 0.90m,
            _ => 0.97m
        };

        // Adjust based on CB if available
        if (request.CB.HasValue)
        {
            var cb = request.CB.Value;
            // CM is typically 0.95-1.00 of CB/CP relationship
            // For simplicity, scale with CB
            if (cb < 0.60m)
                defaultCM -= 0.03m;
            else if (cb > 0.75m)
                defaultCM += 0.01m;
        }

        return new DefaultValueItem
        {
            Value = defaultCM,
            Provenance = $"Typical for {category}",
            Range = "Typical range: 0.90-0.99"
        };
    }

    /// <summary>
    /// Estimates wetted surface area using simplified formula
    /// </summary>
    private DefaultValueItem EstimateWettedSurfaceArea(decimal lpp, decimal b, decimal t, decimal cm)
    {
        // Simplified ITTC formula: S ≈ L × (2T + B) × √CM
        var s = lpp * (2m * t + b) * (decimal)Math.Sqrt((double)cm);

        return new DefaultValueItem
        {
            Value = s,
            Provenance = "Estimated using ITTC formula",
            Range = null
        };
    }
}
