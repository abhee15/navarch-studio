using System;
using Shared.DTOs;

namespace Shared.DTOs.Sizing;

public class PushToHydrostaticsRequestDto
{
    /// <summary>
    /// Optional override name/description when creating the vessel downstream
    /// </summary>
    public string? VesselName { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Optional taxonomy overrides
    /// </summary>
    public string? ShipdCategory { get; set; }
    public string? ShipdType { get; set; }
    public string? ShipdTypeDisplayName { get; set; }
    public string? ShipdBowFamily { get; set; }
    public string? ShipdMidshipFamily { get; set; }
    public string? ShipdSternFamily { get; set; }
    public int? ShipdMaskVersion { get; set; }

    public SourceDesignDto? SourceDesign { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class PushToHydrostaticsResultDto
{
    public Guid VesselId { get; set; }
    public SourceDesignDto? SourceDesign { get; set; }
}

