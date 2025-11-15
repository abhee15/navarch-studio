using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace DataService.Services.Catalog;

/// <summary>
/// Service for importing benchmark hull geometries from CSV files
/// CSV format: hull, station_id, waterline_id, x_over_L, z_over_T, y_over_B2
/// </summary>
public class BenchmarkGeometryImporter
{
    private readonly ILogger<BenchmarkGeometryImporter> _logger;

    public BenchmarkGeometryImporter(ILogger<BenchmarkGeometryImporter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses normalized benchmark hull geometry CSV and converts to actual dimensions
    ///
    /// UNITS: All input and output values are in SI base units (meters)
    /// - CSV contains normalized values (0-1 range)
    /// - Input dimensions (length, beam, draft) must be in meters
    /// - Output geometry (stations, waterlines, offsets) are in meters
    ///
    /// Unit conversion is handled at API boundaries via UnitConversionService
    /// </summary>
    /// <param name="csvStream">CSV stream with normalized offsets (x_over_L, z_over_T, y_over_B2 in range 0-1)</param>
    /// <param name="length">Actual length (L) in meters (SI)</param>
    /// <param name="beam">Actual beam (B) in meters (SI)</param>
    /// <param name="draft">Actual draft (T) in meters (SI)</param>
    /// <param name="hullName">Expected hull name (e.g., "Wigley", "Series60_like", "Prismatic_NPC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed geometry with actual dimensions in meters (SI)</returns>
    public async Task<BenchmarkGeometryResult> ParseFromCsvAsync(
        Stream csvStream,
        decimal length,
        decimal beam,
        decimal draft,
        string? hullName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null,
                IgnoreBlankLines = true
            });

            csv.Context.RegisterClassMap<BenchmarkOffsetCsvRecordMap>();

            var stations = new Dictionary<int, decimal>(); // station_id -> x (actual)
            var waterlines = new Dictionary<int, decimal>(); // waterline_id -> z (actual)
            var offsets = new List<NormalizedOffsetRecord>();

            var rowNumber = 1; // Start at 1 to account for header
            var actualHullName = hullName;

            await foreach (var record in csv.GetRecordsAsync<BenchmarkOffsetCsvRecord>(cancellationToken))
            {
                rowNumber++;

                // Capture hull name from first record if not provided
                if (actualHullName == null)
                {
                    actualHullName = record.Hull;
                }
                else if (record.Hull != actualHullName)
                {
                    _logger.LogWarning("Hull name mismatch at row {Row}: expected '{Expected}', got '{Actual}'",
                        rowNumber, actualHullName, record.Hull);
                }

                // Validate normalized coordinates are in [0, 1] range
                if (record.XOverL < 0 || record.XOverL > 1)
                {
                    _logger.LogWarning("Skipping row {Row}: x_over_L {Value} out of range [0, 1]",
                        rowNumber, record.XOverL);
                    continue;
                }

                if (record.ZOverT < 0 || record.ZOverT > 1)
                {
                    _logger.LogWarning("Skipping row {Row}: z_over_T {Value} out of range [0, 1]",
                        rowNumber, record.ZOverT);
                    continue;
                }

                if (record.YOverB2 < 0 || record.YOverB2 > 1)
                {
                    _logger.LogWarning("Skipping row {Row}: y_over_B2 {Value} out of range [0, 1]",
                        rowNumber, record.YOverB2);
                    continue;
                }

                // Store station positions (normalized -> actual)
                var stationX = record.XOverL * length;
                if (!stations.ContainsKey(record.StationId))
                {
                    stations[record.StationId] = stationX;
                }
                else if (Math.Abs(stations[record.StationId] - stationX) > 0.001m)
                {
                    _logger.LogWarning("Station {StationId} has inconsistent x position at row {Row}",
                        record.StationId, rowNumber);
                }

                // Store waterline positions (normalized -> actual)
                var waterlineZ = record.ZOverT * draft;
                if (!waterlines.ContainsKey(record.WaterlineId))
                {
                    waterlines[record.WaterlineId] = waterlineZ;
                }
                else if (Math.Abs(waterlines[record.WaterlineId] - waterlineZ) > 0.001m)
                {
                    _logger.LogWarning("Waterline {WaterlineId} has inconsistent z position at row {Row}",
                        record.WaterlineId, rowNumber);
                }

                // Store offset (normalized half-breadth -> actual)
                var halfBreadth = record.YOverB2 * (beam / 2m);
                offsets.Add(new NormalizedOffsetRecord
                {
                    StationId = record.StationId,
                    WaterlineId = record.WaterlineId,
                    HalfBreadthY = halfBreadth
                });
            }

            if (stations.Count == 0 || waterlines.Count == 0 || offsets.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No valid geometry data found in CSV. Stations: {stations.Count}, Waterlines: {waterlines.Count}, Offsets: {offsets.Count}");
            }

            // Convert dictionaries to ordered lists
            var stationList = stations.OrderBy(kvp => kvp.Key)
                .Select((kvp, idx) => new StationRecord
                {
                    Index = idx,
                    StationId = kvp.Key,
                    X = kvp.Value
                })
                .ToList();

            var waterlineList = waterlines.OrderBy(kvp => kvp.Key)
                .Select((kvp, idx) => new WaterlineRecord
                {
                    Index = idx,
                    WaterlineId = kvp.Key,
                    Z = kvp.Value
                })
                .ToList();

            _logger.LogInformation(
                "Parsed benchmark geometry '{Hull}': {StationCount} stations, {WaterlineCount} waterlines, {OffsetCount} offsets",
                actualHullName, stationList.Count, waterlineList.Count, offsets.Count);

            return new BenchmarkGeometryResult
            {
                HullName = actualHullName ?? "Unknown",
                Stations = stationList,
                Waterlines = waterlineList,
                Offsets = offsets
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing benchmark geometry CSV");
            throw new InvalidOperationException($"Failed to parse benchmark geometry CSV: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Result of parsing benchmark geometry CSV
/// </summary>
public class BenchmarkGeometryResult
{
    public required string HullName { get; set; }
    public required List<StationRecord> Stations { get; set; }
    public required List<WaterlineRecord> Waterlines { get; set; }
    public required List<NormalizedOffsetRecord> Offsets { get; set; }
}

/// <summary>
/// Station record with index mapping
/// </summary>
public class StationRecord
{
    public int Index { get; set; } // Sequential index (0, 1, 2, ...)
    public int StationId { get; set; } // Original station_id from CSV
    public decimal X { get; set; } // Actual X position in meters
}

/// <summary>
/// Waterline record with index mapping
/// </summary>
public class WaterlineRecord
{
    public int Index { get; set; } // Sequential index (0, 1, 2, ...)
    public int WaterlineId { get; set; } // Original waterline_id from CSV
    public decimal Z { get; set; } // Actual Z position in meters
}

/// <summary>
/// Offset record with actual half-breadth
/// </summary>
public class NormalizedOffsetRecord
{
    public int StationId { get; set; } // Original station_id from CSV
    public int WaterlineId { get; set; } // Original waterline_id from CSV
    public decimal HalfBreadthY { get; set; } // Actual half-breadth in meters
}

// CSV record class
internal class BenchmarkOffsetCsvRecord
{
    public string Hull { get; set; } = string.Empty;
    public int StationId { get; set; }
    public int WaterlineId { get; set; }
    public decimal XOverL { get; set; } // Normalized: 0 to 1
    public decimal ZOverT { get; set; } // Normalized: 0 to 1
    public decimal YOverB2 { get; set; } // Normalized: 0 to 1
}

// CSV Class Map
internal sealed class BenchmarkOffsetCsvRecordMap : ClassMap<BenchmarkOffsetCsvRecord>
{
    public BenchmarkOffsetCsvRecordMap()
    {
        Map(m => m.Hull).Name("hull");
        Map(m => m.StationId).Name("station_id");
        Map(m => m.WaterlineId).Name("waterline_id");
        Map(m => m.XOverL).Name("x_over_L");
        Map(m => m.ZOverT).Name("z_over_T");
        Map(m => m.YOverB2).Name("y_over_B2");
    }
}
