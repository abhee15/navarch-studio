namespace DataService.Services.Resistance;

/// <summary>
/// Configuration for Holtrop-Mennen method
/// Loads from HM_method_config.yaml or uses defaults
/// </summary>
public class HmMethodConfig
{
    public string MethodVersion { get; set; } = "Holtrop-Mennen-1982";

    public FormFactorConfig Friction { get; set; } = new();

    public ResiduaryConfig Residuary { get; set; } = new();

    public CorrelationAllowanceConfig CorrelationAllowance { get; set; } = new();

    public TransomCorrectionConfig TransomCorrection { get; set; } = new();

    public AirResistanceConfig AirResistance { get; set; } = new();

    public AppendagesConfig Appendages { get; set; } = new();

    public class FormFactorConfig
    {
        public string Strategy { get; set; } = "constant_k"; // constant_k | prohaska | none
        public decimal KValue { get; set; } = 0.20m;
    }

    public class ResiduaryConfig
    {
        public string Variant { get; set; } = "standard"; // standard | user_note
        public string UserNote { get; set; } = string.Empty;
    }

    public class CorrelationAllowanceConfig
    {
        public string Model { get; set; } = "ITTC-1978";
    }

    public class TransomCorrectionConfig
    {
        public bool Enabled { get; set; } = true;
        public decimal AreaM2 { get; set; } = 0.0m;
    }

    public class AirResistanceConfig
    {
        public decimal CdDefault { get; set; } = 0.8m;
        public decimal WindageAreaM2 { get; set; } = 0.0m;
    }

    public class AppendagesConfig
    {
        public string Mode { get; set; } = "generic_factor"; // generic_factor | detailed_list
        public decimal GenericFactor { get; set; } = 1.05m;
        public string ListFile { get; set; } = "Appendages_details.csv";
    }

    /// <summary>
    /// Creates default configuration matching HM_method_config.yaml template
    /// </summary>
    public static HmMethodConfig CreateDefault()
    {
        return new HmMethodConfig();
    }
}

