using CsvHelper;
using CsvHelper.Configuration;
using DataService.Data;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Globalization;
using System.Text.Json;

namespace DataService.Services.Catalog;

/// <summary>
/// Imports MIT ShipD parametric hull dataset
/// Processes 45-parameter vectors + geometric measures → principal dimensions
/// </summary>
public class ParametricCatalogImporter
{
    private readonly DataDbContext _context;
    private readonly ILogger<ParametricCatalogImporter> _logger;

    public ParametricCatalogImporter(DataDbContext context, ILogger<ParametricCatalogImporter> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Import parametric hulls from ShipD dataset folder
    /// </summary>
    /// <param name="folderPath">Path to dataset folder (e.g., Constrained_Randomized_Set_1)</param>
    /// <param name="datasetName">Dataset identifier</param>
    /// <param name="maxRows">Limit import (for prototype: 5000)</param>
    /// <param name="skipRows">Sampling strategy (e.g., skip every other row)</param>
    public async Task<ParametricImportResult> ImportFromShipDFolderAsync(
        string folderPath,
        string datasetName,
        int? maxRows = null,
        int skipRows = 1,  // 1 = import all, 2 = every other row
        CancellationToken cancellationToken = default)
    {
        var result = new ParametricImportResult { DatasetName = datasetName };
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting import of {Dataset} from {Path}. MaxRows: {Max}, SkipRows: {Skip}",
                datasetName, folderPath, maxRows ?? -1, skipRows);

            // Step 1: Read Input_Vectors.csv (45 parameters)
            var inputVectorsPath = Path.Combine(folderPath, "Input_Vectors.csv");
            var parametricVectors = ReadInputVectors(inputVectorsPath);

            _logger.LogInformation("Read {Count} parametric vectors from Input_Vectors.csv", parametricVectors.Count);

            // Step 2: Read Geometric Measures (8 CSVs)
            var measuresPath = Path.Combine(folderPath, "GeometricMeasures");
            var geometricMeasures = ReadGeometricMeasures(measuresPath, parametricVectors.Count);

            _logger.LogInformation("Read geometric measures for {Count} hulls", geometricMeasures.Count);

            // Step 3: Process and convert
            var hulls = new List<ParametricHull>();
            int processedCount = 0;

            for (int i = 0; i < parametricVectors.Count; i++)
            {
                // Sampling strategy
                if (i % skipRows != 0)
                    continue;

                if (maxRows.HasValue && processedCount >= maxRows.Value)
                    break;

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var params45 = parametricVectors[i];
                    var measures = geometricMeasures[i];

                    // Compute principal dimensions and coefficients
                    var principal = ComputePrincipalDimensions(params45, measures);

                    // Assess conversion quality
                    var quality = AssessConversionQuality(principal, params45, measures);

                    // Create entity
                    var hull = new ParametricHull
                    {
                        HullId = $"{GetDatasetPrefix(datasetName)}_{i + 1:D5}",
                        DatasetSource = datasetName,
                        RowIndex = i,

                        // Parametric vector (JSONB)
                        ParametricVector = JsonSerializer.Serialize(params45.ToDictionary()),

                        // Key parameters
                        LbRatio = params45.Lb,
                        LsRatio = params45.Ls,
                        BdRatio = params45.Bd,
                        DdRatio = params45.Dd,
                        BsRatio = params45.Bs,

                        // Geometric measures @ design draft
                        VolumeNorm = measures.Volume[4],
                        LcbNorm = measures.LCB[4],
                        VcbNorm = measures.VCB?[4],
                        AreaWpNorm = measures.AreaWP[4],
                        CwCoeff = measures.Cw[4],
                        AreaWsNorm = measures.AreaWS?[4],
                        IxxNorm = measures.Ixx?[4],
                        IyyNorm = measures.Iyy?[4],

                        // All measures (JSONB)
                        GeometricMeasures = JsonSerializer.Serialize(measures.ToDictionary()),

                        // Derived dimensions
                        LppMDerived = principal.Lpp,
                        BeamMDerived = principal.Beam,
                        DraftMDerived = principal.Draft,
                        DepthMDerived = principal.Depth,

                        // Derived coefficients
                        CbDerived = principal.Cb,
                        CpDerived = principal.Cp,
                        CmDerived = principal.Cm,

                        // Quality
                        ConversionQuality = quality.Level,
                        HasValidCoefficients = quality.IsValid,
                        DistortionScore = quality.DistortionScore,

                        ImportedAt = DateTime.UtcNow,
                        DataVersion = 1,
                        IsActive = true
                    };

                    hulls.Add(hull);
                    processedCount++;

                    // Progress logging every 500 hulls
                    if (processedCount % 500 == 0)
                    {
                        _logger.LogInformation("Processed {Count} hulls...", processedCount);
                    }
                }
                catch (Exception ex)
                {
                    result.SkippedRows++;
                    result.Errors.Add($"Row {i}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to process hull at row {Row}", i);
                }
            }

            // Step 4: Bulk insert
            if (hulls.Any())
            {
                _logger.LogInformation("Bulk inserting {Count} parametric hulls...", hulls.Count);

                await _context.ParametricHulls.AddRangeAsync(hulls, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                result.ImportedRows = hulls.Count;
                result.Success = true;
            }

            result.ElapsedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "✅ Parametric import complete. Dataset: {Dataset}, Imported: {Imported}, Skipped: {Skipped}, Time: {Time}ms",
                datasetName, result.ImportedRows, result.SkippedRows, result.ElapsedMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during parametric catalog import");
            result.Success = false;
            result.Errors.Add($"Fatal: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Read Input_Vectors.csv (45 parameters per row)
    /// </summary>
    private List<ParametricVector45> ReadInputVectors(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        return csv.GetRecords<ParametricVector45CsvRow>()
            .Select(row => new ParametricVector45
            {
                LOA = row.LOA,
                Lb = row.Lb,
                Ls = row.Ls,
                Bd = row.Bd,
                Dd = row.Dd,
                Bs = row.Bs,
                WL = row.WL,
                Bc = row.Bc,
                Beta = row.Beta,
                Rc = row.Rc,
                Rk = row.Rk,
                Abow = row.Abow,
                Bbow = row.Bbow,
                BK_z = row.BK_z,
                Kappa_bow = row.Kappa_bow,
                Adel_bow = row.Adel_bow,
                Bdel_bow = row.Bdel_bow,
                Adrft = row.Adrft,
                Bdrft = row.Bdrft,
                Cdrft = row.Cdrft,
                bit_EP_S = row.bit_EP_S,
                bit_EP_T = row.bit_EP_T,
                Atrans = row.Atrans,
                SK_z = row.SK_z,
                Kappa_stern = row.Kappa_stern,
                Adel_stern = row.Adel_stern,
                Bdel_stern = row.Bdel_stern,
                Beta_trans = row.Beta_trans,
                Bc_trans = row.Bc_trans,
                Rc_Trans = row.Rc_Trans,
                Rk_trans = row.Rk_trans,
                bit_BB = row.bit_BB,
                bit_SB = row.bit_SB,
                Lbb = row.Lbb,
                Hbb = row.Hbb,
                Bbb = row.Bbb,
                Lbbm = row.Lbbm,
                Rbb = row.Rbb,
                Kappa_SB = row.Kappa_SB,
                Lsb = row.Lsb,
                HSBOA = row.HSBOA,
                Hsb = row.Hsb,
                Bsb = row.Bsb,
                Lsbm = row.Lsbm,
                Rsb = row.Rsb
            })
            .ToList();
    }

    /// <summary>
    /// Read all geometric measures CSVs and join by row
    /// </summary>
    private List<GeometricMeasures> ReadGeometricMeasures(string measuresPath, int expectedRows)
    {
        // Read each CSV (each has 10 columns for 10 draft ratios)
        var volume = ReadMeasureCsv(Path.Combine(measuresPath, "Volume.csv"));
        var lcb = ReadMeasureCsv(Path.Combine(measuresPath, "LCB.csv"));
        var vcb = File.Exists(Path.Combine(measuresPath, "VCB.csv"))
            ? ReadMeasureCsv(Path.Combine(measuresPath, "VCB.csv"))
            : null;
        var areaWP = ReadMeasureCsv(Path.Combine(measuresPath, "Area_WP.csv"));
        var areaWS = File.Exists(Path.Combine(measuresPath, "Area_WS.csv"))
            ? ReadMeasureCsv(Path.Combine(measuresPath, "Area_WS.csv"))
            : null;
        var cw = ReadMeasureCsv(Path.Combine(measuresPath, "Cw.csv"));
        var ixx = File.Exists(Path.Combine(measuresPath, "Ixx.csv"))
            ? ReadMeasureCsv(Path.Combine(measuresPath, "Ixx.csv"))
            : null;
        var iyy = File.Exists(Path.Combine(measuresPath, "Iyy.csv"))
            ? ReadMeasureCsv(Path.Combine(measuresPath, "Iyy.csv"))
            : null;

        // Join all measures by row index
        var joined = new List<GeometricMeasures>();
        for (int i = 0; i < volume.Count; i++)
        {
            joined.Add(new GeometricMeasures
            {
                Volume = volume[i],
                LCB = lcb[i],
                VCB = vcb?[i],
                AreaWP = areaWP[i],
                AreaWS = areaWS?[i],
                Cw = cw[i],
                Ixx = ixx?[i],
                Iyy = iyy?[i]
            });
        }

        return joined;
    }

    /// <summary>
    /// Read a single geometric measure CSV (10 columns = 10 draft ratios)
    /// </summary>
    private List<decimal[]> ReadMeasureCsv(string filePath)
    {
        using var reader = new StreamReader(filePath);

        // Skip header
        reader.ReadLine();

        var rows = new List<decimal[]>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var values = line.Split(',')
                .Select(v => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0)
                .ToArray();

            rows.Add(values);
        }

        return rows;
    }

    /// <summary>
    /// Compute principal dimensions from 45 params + geometric measures
    /// Using formulas derived from HullParameterization.py and geometric relationships
    /// </summary>
    private PrincipalDimensions ComputePrincipalDimensions(
        ParametricVector45 params45,
        GeometricMeasures measures)
    {
        // Key parameters
        var LOA = params45.LOA;  // Always 10m in ShipD
        var Lb_ratio = params45.Lb;
        var Ls_ratio = params45.Ls;
        var Bd_ratio = params45.Bd;  // Half-beam / LOA
        var Dd_ratio = params45.Dd;  // Depth / LOA
        var WL_ratio = params45.WL;  // Waterline / Dd

        // Geometric measures @ T/Dd = 0.5 (design draft, index 4)
        var Volume_norm = measures.Volume[4];  // Volume/LOA^3
        var LCB_norm = measures.LCB[4];        // LCB/LOA
        var VCB_norm = measures.VCB?[4] ?? 0.5m;
        var AreaWP_norm = measures.AreaWP[4];  // Area_WP/LOA^2
        var Cw = measures.Cw[4];               // Waterplane coefficient

        // Denormalize geometric measures
        var Volume_actual = Volume_norm * (decimal)Math.Pow((double)LOA, 3);
        var AreaWP_actual = AreaWP_norm * (decimal)Math.Pow((double)LOA, 2);
        var LCB_actual = LCB_norm * LOA;

        // Derive principal dimensions
        // Depth = Dd_ratio × LOA
        var Depth = Dd_ratio * LOA;

        // Draft at design condition (T/Dd = 0.5)
        var T_design = 0.5m * Depth;

        // Lpp (typically 95-97% of LOA, use 96%)
        var Lpp = 0.96m * LOA;

        // Beam from waterplane area and Cw
        // Cw = Area_WP / (Lwl × B)
        // Assume Lwl ≈ Lpp for displacement hulls
        var B_from_waterplane = AreaWP_actual / (Cw * Lpp);

        // Beam from Bd_ratio (half-beam at deck)
        var B_from_params = Bd_ratio * 2.0m * LOA;

        // Use average (waterplane gives actual beam, params give deck beam)
        var B = (B_from_waterplane + B_from_params) / 2.0m;

        // Compute form coefficients
        var Cb = Volume_actual / (Lpp * B * T_design);

        // Clamp Cb to valid range
        Cb = Math.Clamp(Cb, 0.25m, 0.98m);

        // Estimate Cp (prismatic coefficient)
        // Typical relationship: Cp = Cb + 0.05 to 0.10
        // For fuller forms (Cb > 0.7), Cp closer to Cb
        // For finer forms (Cb < 0.6), Cp larger relative to Cb
        var Cp = Cb < 0.6m
            ? Math.Min(Cb + 0.10m, 1.0m)
            : Math.Min(Cb + 0.05m, 1.0m);

        // Compute Cm (midship coefficient)
        // Cm = Cb / Cp
        var Cm = Cp > 0 ? Math.Min(Cb / Cp, 1.0m) : 0.95m;

        return new PrincipalDimensions
        {
            Lpp = Lpp,
            Beam = B,
            Draft = T_design,
            Depth = Depth,
            Cb = Cb,
            Cp = Cp,
            Cm = Cm,
            Volume = Volume_actual,
            LCB = LCB_actual,
            VCB = VCB_norm * Depth,
            Cw = Cw
        };
    }

    /// <summary>
    /// Assess quality of parametric → principal conversion
    /// </summary>
    private ConversionQuality AssessConversionQuality(
        PrincipalDimensions principal,
        ParametricVector45 params45,
        GeometricMeasures measures)
    {
        var quality = new ConversionQuality();

        // Check 1: Coefficients in valid range
        if (principal.Cb < 0.25m || principal.Cb > 0.98m)
        {
            quality.Level = "Poor";
            quality.IsValid = false;
            quality.Issues.Add($"Cb out of range: {principal.Cb}");
            return quality;
        }

        // Check 2: Displacement balance
        var volume_from_dimensions = principal.Lpp * principal.Beam * principal.Draft * principal.Cb;
        var volume_error = Math.Abs(volume_from_dimensions - principal.Volume) / principal.Volume;

        quality.DistortionScore = volume_error;

        if (volume_error > 0.10m)  // >10% error
        {
            quality.Level = "Poor";
            quality.Issues.Add($"Volume error: {volume_error:P2}");
        }
        else if (volume_error > 0.05m)  // 5-10% error
        {
            quality.Level = "Fair";
            quality.Issues.Add($"Volume error: {volume_error:P2}");
        }
        else if (volume_error > 0.02m)  // 2-5% error
        {
            quality.Level = "Good";
        }
        else  // <2% error
        {
            quality.Level = "Excellent";
        }

        // Check 3: Reasonable dimensions
        if (principal.Lpp <= 0 || principal.Beam <= 0 || principal.Draft <= 0)
        {
            quality.Level = "Poor";
            quality.IsValid = false;
            quality.Issues.Add("Invalid dimensions (<=0)");
        }

        // Check 4: Reasonable ratios
        var LB_ratio = principal.Lpp / principal.Beam;
        var BT_ratio = principal.Beam / principal.Draft;

        if (LB_ratio < 2.0m || LB_ratio > 15.0m)
        {
            quality.Issues.Add($"Unusual L/B: {LB_ratio:F2}");
            if (quality.Level == "Excellent") quality.Level = "Good";
        }

        if (BT_ratio < 1.5m || BT_ratio > 5.0m)
        {
            quality.Issues.Add($"Unusual B/T: {BT_ratio:F2}");
            if (quality.Level == "Excellent") quality.Level = "Good";
        }

        return quality;
    }

    private string GetDatasetPrefix(string datasetName)
    {
        return datasetName switch
        {
            "Constrained_Randomized_Set_1" => "CS1",
            "Constrained_Randomized_Set_2" => "CS2",
            "Constrained_Randomized_Set_3" => "CS3",
            "Diffusion_Aug_Set_1" => "DA1",
            "Diffusion_Aug_Set_2" => "DA2",
            _ => "UNK"
        };
    }
}

/// <summary>
/// 45-parameter vector from Input_Vectors.csv
/// </summary>
public class ParametricVector45
{
    public decimal LOA { get; set; }
    public decimal Lb { get; set; }
    public decimal Ls { get; set; }
    public decimal Bd { get; set; }
    public decimal Dd { get; set; }
    public decimal Bs { get; set; }
    public decimal WL { get; set; }
    public decimal Bc { get; set; }
    public decimal Beta { get; set; }
    public decimal Rc { get; set; }
    public decimal Rk { get; set; }
    public decimal Abow { get; set; }
    public decimal Bbow { get; set; }
    public decimal BK_z { get; set; }
    public decimal Kappa_bow { get; set; }
    public decimal Adel_bow { get; set; }
    public decimal Bdel_bow { get; set; }
    public decimal Adrft { get; set; }
    public decimal Bdrft { get; set; }
    public decimal Cdrft { get; set; }
    public decimal bit_EP_S { get; set; }
    public decimal bit_EP_T { get; set; }
    public decimal Atrans { get; set; }
    public decimal SK_z { get; set; }
    public decimal Kappa_stern { get; set; }
    public decimal Adel_stern { get; set; }
    public decimal Bdel_stern { get; set; }
    public decimal Beta_trans { get; set; }
    public decimal Bc_trans { get; set; }
    public decimal Rc_Trans { get; set; }
    public decimal Rk_trans { get; set; }
    public decimal bit_BB { get; set; }
    public decimal bit_SB { get; set; }
    public decimal Lbb { get; set; }
    public decimal Hbb { get; set; }
    public decimal Bbb { get; set; }
    public decimal Lbbm { get; set; }
    public decimal Rbb { get; set; }
    public decimal Kappa_SB { get; set; }
    public decimal Lsb { get; set; }
    public decimal HSBOA { get; set; }
    public decimal Hsb { get; set; }
    public decimal Bsb { get; set; }
    public decimal Lsbm { get; set; }
    public decimal Rsb { get; set; }

    public Dictionary<string, decimal> ToDictionary()
    {
        return new Dictionary<string, decimal>
        {
            ["LOA"] = LOA, ["Lb"] = Lb, ["Ls"] = Ls, ["Bd"] = Bd, ["Dd"] = Dd,
            ["Bs"] = Bs, ["WL"] = WL, ["Bc"] = Bc, ["Beta"] = Beta, ["Rc"] = Rc,
            ["Rk"] = Rk, ["Abow"] = Abow, ["Bbow"] = Bbow, ["BK_z"] = BK_z,
            ["Kappa_bow"] = Kappa_bow, ["Adel_bow"] = Adel_bow, ["Bdel_bow"] = Bdel_bow,
            ["Adrft"] = Adrft, ["Bdrft"] = Bdrft, ["Cdrft"] = Cdrft,
            ["bit_EP_S"] = bit_EP_S, ["bit_EP_T"] = bit_EP_T, ["Atrans"] = Atrans,
            ["SK_z"] = SK_z, ["Kappa_stern"] = Kappa_stern, ["Adel_stern"] = Adel_stern,
            ["Bdel_stern"] = Bdel_stern, ["Beta_trans"] = Beta_trans, ["Bc_trans"] = Bc_trans,
            ["Rc_Trans"] = Rc_Trans, ["Rk_trans"] = Rk_trans, ["bit_BB"] = bit_BB,
            ["bit_SB"] = bit_SB, ["Lbb"] = Lbb, ["Hbb"] = Hbb, ["Bbb"] = Bbb,
            ["Lbbm"] = Lbbm, ["Rbb"] = Rbb, ["Kappa_SB"] = Kappa_SB,
            ["Lsb"] = Lsb, ["HSBOA"] = HSBOA, ["Hsb"] = Hsb, ["Bsb"] = Bsb,
            ["Lsbm"] = Lsbm, ["Rsb"] = Rsb
        };
    }
}

/// <summary>
/// CSV row mapping for Input_Vectors.csv (with space-containing headers)
/// </summary>
public class ParametricVector45CsvRow
{
    public decimal LOA { get; set; }
    public decimal Lb { get; set; }
    public decimal Ls { get; set; }
    public decimal Bd { get; set; }
    public decimal Dd { get; set; }
    public decimal Bs { get; set; }
    public decimal WL { get; set; }
    public decimal Bc { get; set; }
    public decimal Beta { get; set; }
    public decimal Rc { get; set; }
    public decimal Rk { get; set; }
    public decimal Abow { get; set; }
    public decimal Bbow { get; set; }
    public decimal BK_z { get; set; }
    public decimal Kappa_bow { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Adel bow")]
    public decimal Adel_bow { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Bdel bow")]
    public decimal Bdel_bow { get; set; }

    public decimal Adrft { get; set; }
    public decimal Bdrft { get; set; }
    public decimal Cdrft { get; set; }
    public decimal bit_EP_S { get; set; }
    public decimal bit_EP_T { get; set; }
    public decimal Atrans { get; set; }
    public decimal SK_z { get; set; }
    public decimal Kappa_stern { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Adel stern")]
    public decimal Adel_stern { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Bdel stern")]
    public decimal Bdel_stern { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Beta trans")]
    public decimal Beta_trans { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Bc trans")]
    public decimal Bc_trans { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Rc Trans")]
    public decimal Rc_Trans { get; set; }

    [CsvHelper.Configuration.Attributes.Name("Rk trans")]
    public decimal Rk_trans { get; set; }

    public decimal bit_BB { get; set; }
    public decimal bit_SB { get; set; }
    public decimal Lbb { get; set; }
    public decimal Hbb { get; set; }
    public decimal Bbb { get; set; }
    public decimal Lbbm { get; set; }
    public decimal Rbb { get; set; }
    public decimal Kappa_SB { get; set; }
    public decimal Lsb { get; set; }
    public decimal HSBOA { get; set; }
    public decimal Hsb { get; set; }
    public decimal Bsb { get; set; }
    public decimal Lsbm { get; set; }
    public decimal Rsb { get; set; }
}

/// <summary>
/// Geometric measures at 10 draft ratios
/// </summary>
public class GeometricMeasures
{
    public decimal[] Volume { get; set; } = new decimal[10];
    public decimal[] LCB { get; set; } = new decimal[10];
    public decimal[]? VCB { get; set; }
    public decimal[] AreaWP { get; set; } = new decimal[10];
    public decimal[]? AreaWS { get; set; }
    public decimal[] Cw { get; set; } = new decimal[10];
    public decimal[]? Ixx { get; set; }
    public decimal[]? Iyy { get; set; }

    public Dictionary<string, decimal[]> ToDictionary()
    {
        var dict = new Dictionary<string, decimal[]>
        {
            ["Volume"] = Volume,
            ["LCB"] = LCB,
            ["AreaWP"] = AreaWP,
            ["Cw"] = Cw
        };

        if (VCB != null) dict["VCB"] = VCB;
        if (AreaWS != null) dict["AreaWS"] = AreaWS;
        if (Ixx != null) dict["Ixx"] = Ixx;
        if (Iyy != null) dict["Iyy"] = Iyy;

        return dict;
    }
}

/// <summary>
/// Computed principal dimensions and form coefficients
/// </summary>
public class PrincipalDimensions
{
    public decimal Lpp { get; set; }
    public decimal Beam { get; set; }
    public decimal Draft { get; set; }
    public decimal Depth { get; set; }
    public decimal Cb { get; set; }
    public decimal? Cp { get; set; }
    public decimal? Cm { get; set; }
    public decimal Volume { get; set; }
    public decimal LCB { get; set; }
    public decimal VCB { get; set; }
    public decimal Cw { get; set; }
}

/// <summary>
/// Conversion quality assessment
/// </summary>
public class ConversionQuality
{
    public string Level { get; set; } = "Good";  // "Excellent", "Good", "Fair", "Poor"
    public bool IsValid { get; set; } = true;
    public decimal DistortionScore { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Result of parametric import operation
/// </summary>
public class ParametricImportResult
{
    public string DatasetName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int ElapsedMs { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, int> QualityDistribution { get; set; } = new();
}
