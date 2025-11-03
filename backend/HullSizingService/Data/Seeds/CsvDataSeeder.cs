using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Data.Seeds;

/// <summary>
/// Seeds reference data from CSV files into the sizing schema
/// </summary>
public class CsvDataSeeder
{
    private readonly SizingDbContext _context;
    private readonly ILogger<CsvDataSeeder> _logger;
    private readonly string _seedDataPath;

    public CsvDataSeeder(SizingDbContext context, ILogger<CsvDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
        _seedDataPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seeds");
    }

    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SEED] Starting seed data import from {Path}", _seedDataPath);

        await SeedHullFamiliesAsync(cancellationToken);
        await SeedIsoContainersAsync(cancellationToken);
        await SeedKpiWeightsAsync(cancellationToken);

        _logger.LogInformation("[SEED] Seed data import complete");
    }

    private async Task SeedHullFamiliesAsync(CancellationToken cancellationToken)
    {
        // Check if already seeded
        var existingCount = await _context.HullFamilyPresets.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("[SEED] Hull families already seeded ({Count} records), skipping", existingCount);
            return;
        }

        var csvPath = Path.Combine(_seedDataPath, "hull_families.csv");
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("[SEED] Hull families CSV not found at {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[SEED] Importing hull families from {Path}", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        });

        var records = csv.GetRecords<HullFamilyCsvRecord>().ToList();

        foreach (var record in records)
        {
            var preset = new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = record.name,
                DisplayName = record.display_name,
                LOverBMin = record.l_over_b_min,
                LOverBMax = record.l_over_b_max,
                BOverTMin = record.b_over_t_min,
                BOverTMax = record.b_over_t_max,
                DOverTMin = record.d_over_t_min,
                DOverTMax = record.d_over_t_max,
                CbMin = record.cb_min,
                CbMax = record.cb_max,
                CpMin = record.cp_min,
                CpMax = record.cp_max,
                CwpMin = record.cwp_min,
                CwpMax = record.cwp_max,
                FnMin = record.fn_min,
                FnMax = record.fn_max,
                GeneratorType = record.generator_type,
                Notes = record.notes,
                IsActive = record.is_active
            };

            _context.HullFamilyPresets.Add(preset);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[SEED] Imported {Count} hull families", records.Count);
    }

    private async Task SeedIsoContainersAsync(CancellationToken cancellationToken)
    {
        var existingCount = await _context.IsoContainers.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("[SEED] ISO containers already seeded ({Count} records), skipping", existingCount);
            return;
        }

        var csvPath = Path.Combine(_seedDataPath, "iso_containers.csv");
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("[SEED] ISO containers CSV not found at {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[SEED] Importing ISO containers from {Path}", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        });

        var records = csv.GetRecords<IsoContainerCsvRecord>().ToList();

        foreach (var record in records)
        {
            var container = new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = record.iso_code,
                LengthMm = (int)(record.length_m * 1000),
                WidthMm = (int)(record.width_m * 1000),
                HeightMm = (int)(record.height_m * 1000),
                MaxGrossKg = (int)record.max_gross_weight_kg
            };

            _context.IsoContainers.Add(container);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[SEED] Imported {Count} ISO container types", records.Count);
    }

    private async Task SeedKpiWeightsAsync(CancellationToken cancellationToken)
    {
        var existingCount = await _context.KpiWeights.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("[SEED] KPI weights already seeded ({Count} records), skipping", existingCount);
            return;
        }

        var csvPath = Path.Combine(_seedDataPath, "kpi_weights.csv");
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("[SEED] KPI weights CSV not found at {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[SEED] Importing KPI weights from {Path}", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        });

        var records = csv.GetRecords<KpiWeightCsvRecord>().ToList();

        foreach (var record in records)
        {
            var weight = new KpiWeight
            {
                Id = Guid.NewGuid(),
                UserId = null, // System default
                Metric = record.metric,
                Weight = record.weight
            };

            _context.KpiWeights.Add(weight);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[SEED] Imported {Count} KPI weights (system defaults)", records.Count);
    }
}

// CSV record classes for mapping
internal record HullFamilyCsvRecord(
    string name,
    string display_name,
    string hull_type,
    decimal l_over_b_min,
    decimal l_over_b_max,
    decimal b_over_t_min,
    decimal b_over_t_max,
    decimal d_over_t_min,
    decimal d_over_t_max,
    decimal cb_min,
    decimal cb_max,
    decimal cp_min,
    decimal cp_max,
    decimal cwp_min,
    decimal cwp_max,
    decimal fn_min,
    decimal fn_max,
    string generator_type,
    string? notes,
    bool is_active
);

internal record IsoContainerCsvRecord(
    string iso_code,
    decimal length_m,
    decimal width_m,
    decimal height_m,
    decimal max_gross_weight_kg,
    string description
);

internal record KpiWeightCsvRecord(
    string metric,
    decimal weight,
    string description
);

