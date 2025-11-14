using System.Collections.Generic;
using Shared.DTOs;

namespace Shared.DTOs.Hydrostatics;

public class HydrostaticsImportRequestDto
{
    public VesselDto Vessel { get; set; } = new();
    public List<StationDto> Stations { get; set; } = new();
    public List<WaterlineDto> Waterlines { get; set; } = new();
    public List<OffsetDto> Offsets { get; set; } = new();
    public string? IdempotencyKey { get; set; }
    public bool CreateDefaultLoadcase { get; set; } = true;
}

