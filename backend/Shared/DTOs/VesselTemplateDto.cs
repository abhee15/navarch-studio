namespace Shared.DTOs;

/// <summary>
/// DTO for vessel creation template
/// </summary>
public class VesselTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VesselDto Preset { get; set; } = new();
}



