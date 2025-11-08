namespace HullSizingService.Data.Seeds;

/// <summary>
/// Orchestrates seeding of all reference data from C# code
/// </summary>
public class ReferenceDataSeeder
{
    private readonly SizingDbContext _context;
    private readonly ILogger<ReferenceDataSeeder> _logger;

    public ReferenceDataSeeder(SizingDbContext context, ILogger<ReferenceDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SEED] Starting reference data import");

        // Seed all reference data from C# code
        await HullFamilySeeder.SeedAsync(_context, _logger, cancellationToken);
        await IsoContainerSeeder.SeedAsync(_context, _logger, cancellationToken);
        await KpiWeightSeeder.SeedAsync(_context, _logger, cancellationToken);

        _logger.LogInformation("[SEED] Reference data import complete");
    }
}
