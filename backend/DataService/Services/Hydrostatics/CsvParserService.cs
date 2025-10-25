using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of CSV parser service
/// </summary>
public class CsvParserService : ICsvParserService
{
    private readonly ILogger<CsvParserService> _logger;

    public CsvParserService(ILogger<CsvParserService> logger)
    {
        _logger = logger;
    }

    public async Task<CombinedGeometryDto> ParseCombinedOffsetsAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Ignore bad data
            });

            var records = new List<CombinedOffsetRecord>();
            await foreach (var record in csv.GetRecordsAsync<CombinedOffsetRecord>(cancellationToken))
            {
                records.Add(record);
            }

            // Extract unique stations
            var stations = records
                .GroupBy(r => r.StationIndex)
                .Select(g => new StationDto
                {
                    StationIndex = g.Key,
                    X = g.First().StationX
                })
                .OrderBy(s => s.StationIndex)
                .ToList();

            // Extract unique waterlines
            var waterlines = records
                .GroupBy(r => r.WaterlineIndex)
                .Select(g => new WaterlineDto
                {
                    WaterlineIndex = g.Key,
                    Z = g.First().WaterlineZ
                })
                .OrderBy(w => w.WaterlineIndex)
                .ToList();

            // Extract offsets
            var offsets = records
                .Select(r => new OffsetDto
                {
                    StationIndex = r.StationIndex,
                    WaterlineIndex = r.WaterlineIndex,
                    HalfBreadthY = r.HalfBreadthY
                })
                .ToList();

            _logger.LogInformation("Parsed combined CSV: {StationCount} stations, {WaterlineCount} waterlines, {OffsetCount} offsets",
                stations.Count, waterlines.Count, offsets.Count);

            return new CombinedGeometryDto
            {
                Stations = stations,
                Waterlines = waterlines,
                Offsets = offsets
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing combined offsets CSV");
            throw new ArgumentException($"Failed to parse CSV: {ex.Message}", ex);
        }
    }

    public async Task<List<StationDto>> ParseStationsAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim
            });

            var stations = new List<StationDto>();
            await foreach (var record in csv.GetRecordsAsync<StationCsvRecord>(cancellationToken))
            {
                stations.Add(new StationDto
                {
                    StationIndex = record.StationIndex,
                    X = record.X
                });
            }

            _logger.LogInformation("Parsed stations CSV: {Count} stations", stations.Count);
            return stations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing stations CSV");
            throw new ArgumentException($"Failed to parse stations CSV: {ex.Message}", ex);
        }
    }

    public async Task<List<WaterlineDto>> ParseWaterlinesAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim
            });

            var waterlines = new List<WaterlineDto>();
            await foreach (var record in csv.GetRecordsAsync<WaterlineCsvRecord>(cancellationToken))
            {
                waterlines.Add(new WaterlineDto
                {
                    WaterlineIndex = record.WaterlineIndex,
                    Z = record.Z
                });
            }

            _logger.LogInformation("Parsed waterlines CSV: {Count} waterlines", waterlines.Count);
            return waterlines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing waterlines CSV");
            throw new ArgumentException($"Failed to parse waterlines CSV: {ex.Message}", ex);
        }
    }

    public async Task<List<OffsetDto>> ParseOffsetsAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim
            });

            var offsets = new List<OffsetDto>();
            await foreach (var record in csv.GetRecordsAsync<OffsetCsvRecord>(cancellationToken))
            {
                offsets.Add(new OffsetDto
                {
                    StationIndex = record.StationIndex,
                    WaterlineIndex = record.WaterlineIndex,
                    HalfBreadthY = record.HalfBreadthY
                });
            }

            _logger.LogInformation("Parsed offsets CSV: {Count} offsets", offsets.Count);
            return offsets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing offsets CSV");
            throw new ArgumentException($"Failed to parse offsets CSV: {ex.Message}", ex);
        }
    }
}

// CSV record classes
internal class CombinedOffsetRecord
{
    public int StationIndex { get; set; }
    public decimal StationX { get; set; }
    public int WaterlineIndex { get; set; }
    public decimal WaterlineZ { get; set; }
    public decimal HalfBreadthY { get; set; }
}

internal class StationCsvRecord
{
    public int StationIndex { get; set; }
    public decimal X { get; set; }
}

internal class WaterlineCsvRecord
{
    public int WaterlineIndex { get; set; }
    public decimal Z { get; set; }
}

internal class OffsetCsvRecord
{
    public int StationIndex { get; set; }
    public int WaterlineIndex { get; set; }
    public decimal HalfBreadthY { get; set; }
}

