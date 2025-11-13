using System;
using System.Text.Json;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.ShipD;

namespace DataService.Services.ShipD;

public class ShipDMetadataService : IShipDMetadataService
{
    private readonly DataDbContext _context;
    private readonly ILogger<ShipDMetadataService> _logger;

    public ShipDMetadataService(DataDbContext context, ILogger<ShipDMetadataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ShipDParameterMetadataDto>> GetParameterMetadataAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.ShipDParameterMetadata
            .OrderBy(p => p.ParameterIndex)
            .ToListAsync(cancellationToken);

        var records = entities
            .Select(p => new ShipDParameterMetadataDto(
                p.Id,
                p.ParameterIndex,
                p.Label,
                p.Group,
                p.Description,
                p.Unit,
                p.Min,
                p.Max,
                p.Mean,
                p.StdDev,
                p.MetadataJson))
            .ToList();

        _logger.LogDebug("[SHIPD_METADATA] Returning {Count} parameter metadata rows", records.Count);
        return records;
    }

    public async Task<IReadOnlyList<ShipDVesselTaxonomyDto>> GetVesselTaxonomyAsync(CancellationToken cancellationToken = default)
    {
        var taxonomyEntities = await _context.ShipDVesselTaxonomies
            .OrderBy(t => t.Category)
            .ThenBy(t => t.DisplayName)
            .ToListAsync(cancellationToken);

        var records = taxonomyEntities
            .Select(t => new ShipDVesselTaxonomyDto(
                t.Id,
                t.Category,
                t.Type,
                t.DisplayName,
                t.Description,
                string.IsNullOrWhiteSpace(t.BowFamiliesJson) ? Array.Empty<string>() : JsonSerializer.Deserialize<List<string>>(t.BowFamiliesJson) ?? new List<string>(),
                string.IsNullOrWhiteSpace(t.MidshipFamiliesJson) ? Array.Empty<string>() : JsonSerializer.Deserialize<List<string>>(t.MidshipFamiliesJson) ?? new List<string>(),
                string.IsNullOrWhiteSpace(t.SternFamiliesJson) ? Array.Empty<string>() : JsonSerializer.Deserialize<List<string>>(t.SternFamiliesJson) ?? new List<string>(),
                t.MaskVersion,
                t.AdditionalParametersJson))
            .ToList();

        _logger.LogDebug("[SHIPD_METADATA] Returning {Count} vessel taxonomy rows", records.Count);
        return records;
    }
}
