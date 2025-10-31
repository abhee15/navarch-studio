namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for seeding template vessels into the database
/// </summary>
public interface ITemplateVesselSeeder
{
    /// <summary>
    /// Seeds the hydrostatics template vessel if it doesn't exist
    /// </summary>
    Task SeedHydrostaticsTemplateAsync(CancellationToken cancellationToken = default);
}
