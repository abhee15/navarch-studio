using Shared.Constants;
using Shared.HullGenerators.Models;

namespace Shared.HullGenerators.Validation;

/// <summary>
/// Validates generated offsets against target form coefficients
/// </summary>
public static class OffsetValidator
{
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Validate generated offsets against targets
    /// </summary>
    public static ValidationResult Validate(
        FormCoefficients computed,
        decimal targetCb,
        decimal targetCp,
        decimal targetCm,
        decimal targetCwp,
        decimal targetLcbPercent,
        decimal length,
        decimal beam,
        decimal draft)
    {
        var result = new ValidationResult { IsValid = true };

        // Validate Cb
        decimal cbError = Math.Abs(computed.Cb - targetCb) / targetCb * 100;
        if (cbError > BSRAConstants.ValidationTolerances.CbTolerancePercent)
        {
            result.IsValid = false;
            result.Errors.Add(
                $"Cb error: {cbError:F2}% (target: {targetCb:F4}, computed: {computed.Cb:F4}, tolerance: {BSRAConstants.ValidationTolerances.CbTolerancePercent}%)");
        }

        // Validate Cp
        decimal cpError = Math.Abs(computed.Cp - targetCp) / targetCp * 100;
        if (cpError > BSRAConstants.ValidationTolerances.CbTolerancePercent) // Use same tolerance as Cb
        {
            result.Warnings.Add(
                $"Cp error: {cpError:F2}% (target: {targetCp:F4}, computed: {computed.Cp:F4})");
        }

        // Validate Cm
        decimal cmError = Math.Abs(computed.Cm - targetCm) / targetCm * 100;
        if (cmError > BSRAConstants.ValidationTolerances.CbTolerancePercent)
        {
            result.Warnings.Add(
                $"Cm error: {cmError:F2}% (target: {targetCm:F4}, computed: {computed.Cm:F4})");
        }

        // Validate Cwp
        decimal cwpError = Math.Abs(computed.Cwp - targetCwp) / targetCwp * 100;
        if (cwpError > BSRAConstants.ValidationTolerances.WaterplaneAreaTolerancePercent)
        {
            result.Warnings.Add(
                $"Cwp error: {cwpError:F2}% (target: {targetCwp:F4}, computed: {computed.Cwp:F4})");
        }

        // Validate LCB
        decimal lcbError = Math.Abs(computed.LcbPercent - targetLcbPercent);
        if (lcbError > BSRAConstants.ValidationTolerances.LcbTolerancePercent)
        {
            result.Warnings.Add(
                $"LCB error: {lcbError:F2}% (target: {targetLcbPercent:F2}%, computed: {computed.LcbPercent:F2}%, tolerance: {BSRAConstants.ValidationTolerances.LcbTolerancePercent}%)");
        }

        // Validate volume
        decimal targetVolume = targetCb * length * beam * draft;
        decimal volumeError = Math.Abs(computed.Volume - targetVolume) / targetVolume * 100;
        if (volumeError > BSRAConstants.ValidationTolerances.VolumeTolerancePercent)
        {
            result.IsValid = false;
            result.Errors.Add(
                $"Volume error: {volumeError:F2}% (target: {targetVolume:F2} m³, computed: {computed.Volume:F2} m³, tolerance: {BSRAConstants.ValidationTolerances.VolumeTolerancePercent}%)");
        }

        return result;
    }
}
