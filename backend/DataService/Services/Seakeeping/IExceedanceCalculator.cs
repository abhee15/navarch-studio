namespace DataService.Services.Seakeeping;

/// <summary>
/// Exceedance probability calculator using Rayleigh distribution.
/// </summary>
public interface IExceedanceCalculator
{
    /// <summary>
    /// Calculate exceedance probabilities for motion thresholds.
    /// </summary>
    /// <param name="significantResponse">Significant response value (1/3 highest)</param>
    /// <param name="thresholds">Array of threshold values</param>
    /// <returns>Dictionary of threshold -> probability</returns>
    Dictionary<double, double> CalculateExceedanceProbabilities(
        double significantResponse,
        double[] thresholds
    );
}
