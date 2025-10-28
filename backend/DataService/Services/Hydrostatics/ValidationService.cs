using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of validation service for hydrostatics data
/// </summary>
public class ValidationService : IValidationService
{
    public ValidationResult ValidateStations(List<StationDto> stations)
    {
        var errors = new List<ValidationError>();

        if (stations == null || stations.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Field = "Stations",
                Message = "At least 3 stations are required"
            });
            return ValidationResult.Failure(errors.ToArray());
        }

        if (stations.Count < 3)
        {
            errors.Add(new ValidationError
            {
                Field = "Stations",
                Message = $"At least 3 stations are required, found {stations.Count}"
            });
        }

        // Check for monotonically increasing X values
        for (int i = 0; i < stations.Count - 1; i++)
        {
            if (stations[i].X >= stations[i + 1].X)
            {
                errors.Add(new ValidationError
                {
                    Field = "Stations",
                    Message = $"Station X values must be monotonically increasing. Found {stations[i].X} >= {stations[i + 1].X}",
                    Row = i + 1
                });
            }
        }

        // Check for non-negative values
        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].X < 0)
            {
                errors.Add(new ValidationError
                {
                    Field = "Stations",
                    Message = $"Station X values must be non-negative. Found {stations[i].X}",
                    Row = i
                });
            }
        }

        // Check for duplicate indices
        var duplicateIndices = stations
            .GroupBy(s => s.StationIndex)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIndices.Any())
        {
            errors.Add(new ValidationError
            {
                Field = "Stations",
                Message = $"Duplicate station indices found: {string.Join(", ", duplicateIndices)}"
            });
        }

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    public ValidationResult ValidateWaterlines(List<WaterlineDto> waterlines)
    {
        var errors = new List<ValidationError>();

        if (waterlines == null || waterlines.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Field = "Waterlines",
                Message = "At least 3 waterlines are required"
            });
            return ValidationResult.Failure(errors.ToArray());
        }

        if (waterlines.Count < 3)
        {
            errors.Add(new ValidationError
            {
                Field = "Waterlines",
                Message = $"At least 3 waterlines are required, found {waterlines.Count}"
            });
        }

        // Check for monotonically increasing Z values
        for (int i = 0; i < waterlines.Count - 1; i++)
        {
            if (waterlines[i].Z >= waterlines[i + 1].Z)
            {
                errors.Add(new ValidationError
                {
                    Field = "Waterlines",
                    Message = $"Waterline Z values must be monotonically increasing. Found {waterlines[i].Z} >= {waterlines[i + 1].Z}",
                    Row = i + 1
                });
            }
        }

        // Check for non-negative values
        for (int i = 0; i < waterlines.Count; i++)
        {
            if (waterlines[i].Z < 0)
            {
                errors.Add(new ValidationError
                {
                    Field = "Waterlines",
                    Message = $"Waterline Z values must be non-negative. Found {waterlines[i].Z}",
                    Row = i
                });
            }
        }

        // Check for duplicate indices
        var duplicateIndices = waterlines
            .GroupBy(w => w.WaterlineIndex)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIndices.Any())
        {
            errors.Add(new ValidationError
            {
                Field = "Waterlines",
                Message = $"Duplicate waterline indices found: {string.Join(", ", duplicateIndices)}"
            });
        }

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    public ValidationResult ValidateOffsets(List<OffsetDto> offsets, int stationCount, int waterlineCount)
    {
        var errors = new List<ValidationError>();

        if (offsets == null || offsets.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Field = "Offsets",
                Message = "Offsets data is required"
            });
            return ValidationResult.Failure(errors.ToArray());
        }

        // Check for non-negative half-breadths
        foreach (var offset in offsets)
        {
            if (offset.HalfBreadthY < 0)
            {
                errors.Add(new ValidationError
                {
                    Field = "Offsets",
                    Message = $"Half-breadth must be non-negative at station {offset.StationIndex}, waterline {offset.WaterlineIndex}. Found {offset.HalfBreadthY}",
                    Row = offset.StationIndex,
                    Column = offset.WaterlineIndex
                });
            }
        }

        // Check that all station/waterline combinations are present
        var expectedCount = stationCount * waterlineCount;
        if (offsets.Count != expectedCount)
        {
            var missing = new List<(int station, int waterline)>();
            for (int s = 0; s < stationCount; s++)
            {
                for (int w = 0; w < waterlineCount; w++)
                {
                    if (!offsets.Any(o => o.StationIndex == s && o.WaterlineIndex == w))
                    {
                        missing.Add((s, w));
                        if (missing.Count >= 10) break; // Limit error reporting
                    }
                }
                if (missing.Count >= 10) break;
            }

            if (missing.Any())
            {
                errors.Add(new ValidationError
                {
                    Field = "Offsets",
                    Message = $"Missing offsets for {missing.Count} station/waterline combinations. First missing: Station {missing[0].station}, Waterline {missing[0].waterline}"
                });
            }
        }

        // Check for duplicates
        var duplicates = offsets
            .GroupBy(o => new { o.StationIndex, o.WaterlineIndex })
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Take(10)
            .ToList();

        if (duplicates.Any())
        {
            errors.Add(new ValidationError
            {
                Field = "Offsets",
                Message = $"Duplicate offsets found for {duplicates.Count} locations. First: Station {duplicates[0].StationIndex}, Waterline {duplicates[0].WaterlineIndex}"
            });
        }

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    public ValidationResult ValidateVessel(VesselDto vessel)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(vessel.Name))
        {
            errors.Add(new ValidationError
            {
                Field = "Name",
                Message = "Vessel name is required"
            });
        }

        if (vessel.Lpp <= 0)
        {
            errors.Add(new ValidationError
            {
                Field = "Lpp",
                Message = "Length between perpendiculars must be positive"
            });
        }

        if (vessel.Beam <= 0)
        {
            errors.Add(new ValidationError
            {
                Field = "Beam",
                Message = "Beam must be positive"
            });
        }

        if (vessel.DesignDraft <= 0)
        {
            errors.Add(new ValidationError
            {
                Field = "DesignDraft",
                Message = "Design draft must be positive"
            });
        }

        // Reasonable range checks
        if (vessel.Lpp > 500)
        {
            errors.Add(new ValidationError
            {
                Field = "Lpp",
                Message = "Length exceeds reasonable range (max 500m for this phase)"
            });
        }

        if (vessel.Beam > vessel.Lpp)
        {
            errors.Add(new ValidationError
            {
                Field = "Beam",
                Message = "Beam should not exceed length"
            });
        }

        if (vessel.DesignDraft > vessel.Beam)
        {
            errors.Add(new ValidationError
            {
                Field = "DesignDraft",
                Message = "Design draft should not exceed beam"
            });
        }

        return errors.Any()
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}

