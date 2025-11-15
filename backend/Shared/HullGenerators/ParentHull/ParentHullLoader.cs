using System.Globalization;
using Microsoft.Extensions.Logging;
using Shared.HullGenerators.Models;

namespace Shared.HullGenerators.ParentHull;

/// <summary>
/// Loads parent hull data from CSV registry and offset tables
/// Caches loaded hulls in memory for performance
/// </summary>
public class ParentHullLoader
{
    private readonly ILogger<ParentHullLoader>? _logger;
    private static readonly Dictionary<string, ParentHullData> _cache = new();
    private static readonly object _lock = new();

    public ParentHullLoader(ILogger<ParentHullLoader>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if a parent hull is available for the given vessel type and Cb
    /// </summary>
    public static bool HasParentHull(string? vesselType, decimal cb)
    {
        if (string.IsNullOrWhiteSpace(vesselType))
            return false;

        var registry = LoadRegistry();
        return registry.Any(h =>
            string.Equals(h.VesselType, vesselType, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(h.Cb - cb) < 0.01m); // Within 0.01 tolerance
    }

    /// <summary>
    /// Load parent hull for vessel type and Cb
    /// Returns closest match if exact Cb not available
    /// </summary>
    public ParentHullData LoadParentHull(string vesselType, decimal cbTarget)
    {
        var cacheKey = $"{vesselType}_{cbTarget:F2}";

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _logger?.LogDebug("Returning cached parent hull: {VesselType}, Cb={Cb}", vesselType, cbTarget);
                return cached;
            }
        }

        var registry = LoadRegistry();

        // Filter by vessel type
        var candidates = registry
            .Where(h => string.Equals(h.VesselType, vesselType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                $"No parent hulls found for vessel type: {vesselType}");
        }

        // Find closest Cb match
        var bestMatch = candidates
            .OrderBy(h => Math.Abs(h.Cb - cbTarget))
            .First();

        _logger?.LogInformation(
            "Loading parent hull: {VesselType}, Cb={Cb} (target: {TargetCb}, source: {Source})",
            vesselType, bestMatch.Cb, cbTarget, bestMatch.Source);

        // Load offset table
        var offsets = LoadOffsetTable(vesselType, bestMatch.Cb);
        bestMatch.Stations = offsets.Stations;
        bestMatch.Waterlines = offsets.Waterlines;
        bestMatch.Offsets = offsets.Offsets;

        lock (_lock)
        {
            _cache[cacheKey] = bestMatch;
        }

        return bestMatch;
    }

    /// <summary>
    /// Load parent hull registry from CSV
    /// </summary>
    private static List<ParentHullData> LoadRegistry()
    {
        var registryPath = FindDataFile("Data/BSRA/parent_hulls_registry.csv");

        if (!File.Exists(registryPath))
        {
            throw new FileNotFoundException(
                $"Parent hull registry not found. Searched: {registryPath}");
        }

        var registry = new List<ParentHullData>();
        var lines = File.ReadAllLines(registryPath);

        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 10)
                continue;

            registry.Add(new ParentHullData
            {
                VesselType = parts[0].Trim(),
                Cb = decimal.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                Lbp = decimal.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                B = decimal.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                D = decimal.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                T = decimal.Parse(parts[5].Trim(), CultureInfo.InvariantCulture),
                Cm = decimal.Parse(parts[6].Trim(), CultureInfo.InvariantCulture),
                Cw = decimal.Parse(parts[7].Trim(), CultureInfo.InvariantCulture),
                LcbPercent = decimal.Parse(parts[8].Trim(), CultureInfo.InvariantCulture),
                Source = parts[9].Trim(),
                Notes = parts.Length > 10 ? parts[10].Trim() : null
            });
        }

        return registry;
    }

    /// <summary>
    /// Find data file using multiple search strategies
    /// Supports both development and production deployment scenarios
    /// </summary>
    private static string FindDataFile(string relativePath)
    {
        // Strategy 1: Check for explicit data directory from environment variable (production)
        var dataDir = Environment.GetEnvironmentVariable("NAVARCH_DATA_DIR");
        if (!string.IsNullOrEmpty(dataDir))
        {
            var envPath = Path.Combine(dataDir, relativePath);
            if (File.Exists(envPath))
                return envPath;
        }

        // Strategy 2: Relative to assembly location (production - when deployed)
        var assemblyLocation = typeof(ParentHullLoader).Assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                // Try directly in assembly directory
                var path2 = Path.Combine(assemblyDir, relativePath);
                if (File.Exists(path2))
                    return path2;

                // Try in Data subdirectory of assembly location
                var dataPath = Path.Combine(assemblyDir, "Data", relativePath);
                if (File.Exists(dataPath))
                    return dataPath;

                // Try going up to find Shared project (development)
                var sharedDir = Path.GetDirectoryName(assemblyDir);
                if (!string.IsNullOrEmpty(sharedDir))
                {
                    var path3 = Path.Combine(sharedDir, relativePath);
                    if (File.Exists(path3))
                        return path3;
                }
            }
        }

        // Strategy 3: Relative to AppDomain base directory (production)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir))
        {
            var path4 = Path.Combine(baseDir, relativePath);
            if (File.Exists(path4))
                return path4;

            // Try in Data subdirectory
            var baseDataPath = Path.Combine(baseDir, "Data", relativePath);
            if (File.Exists(baseDataPath))
                return baseDataPath;
        }

        // Strategy 4: Relative to current working directory (development)
        var currentDir = Directory.GetCurrentDirectory();
        var path1 = Path.Combine(currentDir, relativePath);
        if (File.Exists(path1))
            return path1;

        // Strategy 5: Try from solution root (for development)
        var solutionRoot = currentDir;
        for (int i = 0; i < 5; i++)
        {
            var testPath = Path.Combine(solutionRoot, "backend", "Shared", relativePath);
            if (File.Exists(testPath))
                return testPath;

            var parent = Directory.GetParent(solutionRoot);
            if (parent == null)
                break;
            solutionRoot = parent.FullName;
        }

        // Return the first attempted path for error message
        return path1;
    }

    /// <summary>
    /// Load offset table from CSV
    /// </summary>
    private static (List<decimal> Stations, List<decimal> Waterlines, List<List<decimal>> Offsets) LoadOffsetTable(
        string vesselType, decimal cb)
    {
        // Generate filename: e.g., "product_carrier_cb080_offsets.csv" for Cb=0.80
        var cbInt = (int)(cb * 100);
        var fileName = $"{vesselType}_cb{cbInt:D3}_offsets.csv";
        var offsetPath = FindDataFile($"Data/BSRA/parent_hulls/{fileName}");

        if (!File.Exists(offsetPath))
        {
            throw new FileNotFoundException(
                $"Parent hull offset table not found: {fileName}. Searched: {offsetPath}");
        }

        var lines = File.ReadAllLines(offsetPath);
        if (lines.Length < 2)
            throw new InvalidDataException("Offset table must have at least header and one data row");

        // Parse header to get waterline names
        var header = lines[0].Split(',');
        var waterlineNames = new List<string>();
        for (int i = 1; i < header.Length; i++) // Skip "station" column
        {
            waterlineNames.Add(header[i].Trim());
        }

        // Parse waterline heights from names (e.g., "wl_1" -> 1.0, "wl_16.4" -> 16.4)
        var waterlines = waterlineNames.Select(name =>
        {
            var wlPart = name.Replace("wl_", "");
            return decimal.Parse(wlPart, CultureInfo.InvariantCulture);
        }).ToList();

        // Parse data rows
        // Stations in CSV are normalized 0-10 (BSRA standard), not actual meters
        var stations = new List<decimal>();
        var offsets = new List<List<decimal>>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 2)
                continue;

            // Station is normalized 0-10 (BSRA standard)
            stations.Add(decimal.Parse(parts[0].Trim(), CultureInfo.InvariantCulture));

            var rowOffsets = new List<decimal>();
            for (int j = 1; j < parts.Length; j++)
            {
                rowOffsets.Add(decimal.Parse(parts[j].Trim(), CultureInfo.InvariantCulture));
            }
            offsets.Add(rowOffsets);
        }

        return (stations, waterlines, offsets);
    }
}
