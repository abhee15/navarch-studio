using System.Collections.Generic;
using System.Text.Json;
using DataService.Data.ShipD;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataService.Data.Seeds;

public class ShipDMetadataSeeder
{
    private readonly DataDbContext _context;
    private readonly ILogger<ShipDMetadataSeeder> _logger;

    public ShipDMetadataSeeder(DataDbContext context, ILogger<ShipDMetadataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedParameterMetadataAsync(cancellationToken);
        await SeedVesselTaxonomyAsync(cancellationToken);
    }

    private async Task SeedParameterMetadataAsync(CancellationToken cancellationToken)
    {
        var seeds = ShipDMetadataDefaults.ParameterMetadata;
        var existing = await _context.ShipDParameterMetadata
            .ToDictionaryAsync(p => p.ParameterIndex, cancellationToken);

        var now = DateTime.UtcNow;
        var created = 0;
        var updated = 0;

        foreach (var seed in seeds)
        {
            if (!existing.TryGetValue(seed.ParameterIndex, out var entity))
            {
                entity = new ShipDParameterMetadata
                {
                    Id = Guid.NewGuid(),
                    ParameterIndex = seed.ParameterIndex,
                    CreatedAt = now
                };

                _context.ShipDParameterMetadata.Add(entity);
                existing[seed.ParameterIndex] = entity;
                created++;
            }
            else
            {
                updated++;
            }

            entity.Label = seed.Label;
            entity.Group = seed.Group;
            entity.Description = seed.Description;
            entity.Unit = seed.Unit;
            entity.Min = seed.Min;
            entity.Max = seed.Max;
            entity.Mean = seed.Mean;
            entity.StdDev = seed.StdDev;
            entity.MetadataJson = seed.MetadataJson;
            entity.UpdatedAt = now;
        }

        if (created > 0 || updated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[SHIPD] Parameter metadata seeded. Created: {Created}, Updated: {Updated}", created, updated);
        }
        else
        {
            _logger.LogInformation("[SHIPD] Parameter metadata already up to date");
        }
    }

    private async Task SeedVesselTaxonomyAsync(CancellationToken cancellationToken)
    {
        var seeds = ShipDMetadataDefaults.VesselTaxonomy;
        var existing = await _context.ShipDVesselTaxonomies
            .ToDictionaryAsync(t => (t.Category, t.Type), cancellationToken);

        var now = DateTime.UtcNow;
        var created = 0;
        var updated = 0;

        foreach (var seed in seeds)
        {
            if (!existing.TryGetValue((seed.Category, seed.Type), out var entity))
            {
                entity = new ShipDVesselTaxonomy
                {
                    Id = Guid.NewGuid(),
                    Category = seed.Category,
                    Type = seed.Type,
                    CreatedAt = now
                };

                _context.ShipDVesselTaxonomies.Add(entity);
                existing[(seed.Category, seed.Type)] = entity;
                created++;
            }
            else
            {
                updated++;
            }

            entity.DisplayName = seed.DisplayName;
            entity.Description = seed.Description;
            entity.BowFamiliesJson = JsonSerializer.Serialize(seed.BowFamilies);
            entity.MidshipFamiliesJson = JsonSerializer.Serialize(seed.MidshipFamilies);
            entity.SternFamiliesJson = JsonSerializer.Serialize(seed.SternFamilies);
            entity.MaskVersion = seed.MaskVersion;
            entity.AdditionalParametersJson = seed.AdditionalParametersJson;
            entity.UpdatedAt = now;
        }

        if (created > 0 || updated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[SHIPD] Vessel taxonomy seeded. Created: {Created}, Updated: {Updated}", created, updated);
        }
        else
        {
            _logger.LogInformation("[SHIPD] Vessel taxonomy already up to date");
        }
    }
}
