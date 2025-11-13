namespace HullSizingService.Services.Geometry;

/// <summary>
/// DTO for hull sections (transverse stations)
/// </summary>
public record HullSectionsDto
{
    /// <summary>
    /// List of stations, each containing offsets at various heights
    /// </summary>
    public List<HullStationDto> Stations { get; init; } = new();

    /// <summary>
    /// Longitudinal positions of stations (0 = aft, 1 = forward)
    /// </summary>
    public List<decimal> StationPositions { get; init; } = new();
}

/// <summary>
/// DTO for a single hull station (transverse section)
/// </summary>
public record HullStationDto
{
    /// <summary>
    /// Station position along LOA (0 = aft, 1 = forward)
    /// </summary>
    public decimal Position { get; init; }

    /// <summary>
    /// Half-breadths at various heights (heights from keel, in meters)
    /// Key: height from keel (m), Value: half-breadth (m)
    /// </summary>
    public Dictionary<decimal, decimal> Offsets { get; init; } = new();

    /// <summary>
    /// Whether this station includes bulb geometry
    /// </summary>
    public bool HasBulb { get; init; }

    /// <summary>
    /// Bulb offsets if present (height from keel -> half-breadth)
    /// </summary>
    public Dictionary<decimal, decimal>? BulbOffsets { get; init; }
}

/// <summary>
/// DTO for 3D hull mesh
/// </summary>
public record HullMesh3DDto
{
    /// <summary>
    /// Vertices: List of [x, y, z] coordinates
    /// </summary>
    public List<List<decimal>> Vertices { get; init; } = new();

    /// <summary>
    /// Faces: List of vertex indices forming triangles
    /// </summary>
    public List<List<int>> Faces { get; init; } = new();

    /// <summary>
    /// Normals: List of [nx, ny, nz] normal vectors (optional, for shading)
    /// </summary>
    public List<List<decimal>>? Normals { get; init; }
}

/// <summary>
/// DTO for ShipD parameter validation result
/// </summary>
public record ShipDValidationResultDto
{
    /// <summary>
    /// Whether the parameter vector is valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}

