using Microsoft.Extensions.Logging;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Calculates exceedance probabilities using Rayleigh distribution.
/// </summary>
public class ExceedanceCalculator : IExceedanceCalculator
{
    private readonly ILogger<ExceedanceCalculator> _logger;

    public ExceedanceCalculator(ILogger<ExceedanceCalculator> logger)
    {
        _logger = logger;
    }

    public Dictionary<double, double> CalculateExceedanceProbabilities(
        double significantResponse,
        double[] thresholds)
    {
        _logger.LogDebug(
            "Calculating exceedance probabilities for significant response {Response}",
            significantResponse
        );

        var probabilities = new Dictionary<double, double>();

        // Rayleigh distribution parameter: σ = x₁/₃ / 4
        var sigma = significantResponse / 4.0;

        if (sigma <= 0)
        {
            _logger.LogWarning("Significant response is zero or negative, returning zero probabilities");
            foreach (var threshold in thresholds)
            {
                probabilities[threshold] = 0;
            }
            return probabilities;
        }

        // Rayleigh distribution: P(x > threshold) = exp(-(threshold²)/(2σ²))
        foreach (var threshold in thresholds)
        {
            if (threshold < 0)
            {
                probabilities[threshold] = 1.0; // Always exceeds negative threshold
            }
            else
            {
                var prob = Math.Exp(-Math.Pow(threshold, 2) / (2 * Math.Pow(sigma, 2)));
                probabilities[threshold] = prob;
            }
        }

        return probabilities;
    }
}
