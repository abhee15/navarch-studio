namespace Shared.Models.Sizing;

public class IsoContainer
{
    public Guid Id { get; set; }
    public string ContainerType { get; set; } = null!;
    public int LengthMm { get; set; }
    public int WidthMm { get; set; }
    public int HeightMm { get; set; }
    public int MaxGrossKg { get; set; }
}

