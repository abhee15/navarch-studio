using Shared.DTOs;

namespace DataService.Services.Resistance;

/// <summary>
/// Service for calculating delivered and installed power from effective power
/// </summary>
public class PowerCalculationService
{
    private readonly ILogger<PowerCalculationService> _logger;

    public PowerCalculationService(ILogger<PowerCalculationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates delivered power (DHP) and installed power (P_inst) from effective power (EHP)
    /// DHP = EHP / etaD
    /// P_inst = DHP * (1 + serviceMargin/100)
    /// 
    /// If decomposed efficiencies are provided: etaD = etaH * etaR * etaO
    /// </summary>
    public PowerCurveResult CalculatePowerCurves(PowerCurveRequest request)
    {
        _logger.LogInformation(
            "Computing power curves for {Count} speeds with SM={ServiceMargin}%",
            request.EffectivePower.Count,
            request.ServiceMargin);

        if (request.EffectivePower.Count != request.SpeedGrid.Count)
        {
            throw new ArgumentException(
                "EffectivePower and SpeedGrid must have the same length",
                nameof(request));
        }

        // Calculate overall propulsive efficiency
        decimal etaD;
        if (request.EtaD.HasValue)
        {
            etaD = request.EtaD.Value;
            _logger.LogInformation("Using provided etaD = {EtaD}", etaD);
        }
        else if (request.EtaH.HasValue && request.EtaR.HasValue && request.EtaO.HasValue)
        {
            etaD = request.EtaH.Value * request.EtaR.Value * request.EtaO.Value;
            _logger.LogInformation(
                "Calculated etaD = {EtaH} * {EtaR} * {EtaO} = {EtaD}",
                request.EtaH.Value,
                request.EtaR.Value,
                request.EtaO.Value,
                etaD);
        }
        else
        {
            // Default efficiency if not provided
            etaD = 0.65m;
            _logger.LogWarning("No efficiency provided, using default etaD = 0.65");
        }

        // Validate efficiency range
        if (etaD <= 0 || etaD > 1)
        {
            throw new ArgumentException(
                $"Efficiency etaD must be between 0 and 1, got {etaD}",
                nameof(request));
        }

        // Validate service margin
        if (request.ServiceMargin < 0 || request.ServiceMargin > 30)
        {
            throw new ArgumentException(
                $"Service margin must be between 0% and 30%, got {request.ServiceMargin}%",
                nameof(request));
        }

        var result = new PowerCurveResult
        {
            SpeedGrid = request.SpeedGrid.ToList(),
            EffectivePower = request.EffectivePower.ToList(),
            DeliveredPower = new List<decimal>(),
            InstalledPower = new List<decimal>(),
            ServiceMargin = request.ServiceMargin,
            EtaD = etaD
        };

        // Calculate DHP and P_inst for each speed point
        for (int i = 0; i < request.EffectivePower.Count; i++)
        {
            decimal ehp = request.EffectivePower[i];
            
            // DHP = EHP / etaD
            decimal dhp = ehp / etaD;
            
            // P_inst = DHP * (1 + serviceMargin/100)
            decimal serviceFactor = 1m + request.ServiceMargin / 100m;
            decimal pInst = dhp * serviceFactor;
            
            result.DeliveredPower.Add(dhp);
            result.InstalledPower.Add(pInst);
        }

        _logger.LogInformation(
            "Power curves computed: DHP range {MinDHP}-{MaxDHP} kW, P_inst range {MinPInst}-{MaxPInst} kW",
            result.DeliveredPower.Min(),
            result.DeliveredPower.Max(),
            result.InstalledPower.Min(),
            result.InstalledPower.Max());

        return result;
    }
}

