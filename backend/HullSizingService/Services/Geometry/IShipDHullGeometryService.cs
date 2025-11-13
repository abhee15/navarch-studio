using Shared.DTOs.ShipD;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Service for generating hull geometry from ShipD parameter vectors.
/// Converts the 45-parameter ShipD vector into actual hull offsets, sections, and 3D meshes.
/// </summary>
public interface IShipDHullGeometryService
{
    /// <summary>
    /// Generates hull sections from ShipD parameter vector.
    /// Returns a collection of transverse sections (stations) with offsets.
    /// </summary>
    Task<HullSectionsDto> GenerateSectionsAsync(
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        int stationCount = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates 3D hull mesh from ShipD parameters.
    /// Returns a mesh representation suitable for visualization.
    /// </summary>
    Task<HullMesh3DDto> GenerateMeshAsync(
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        int longitudinalSegments = 60,
        int verticalSegments = 40,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates ShipD parameter vector against geometric constraints.
    /// Ensures watertightness, no self-intersection, and valid parameter ranges.
    /// </summary>
    Task<ShipDValidationResultDto> ValidateParametersAsync(
        decimal[] shipdVector,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        CancellationToken cancellationToken = default);
}

