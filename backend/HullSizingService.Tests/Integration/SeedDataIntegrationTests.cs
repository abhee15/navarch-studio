using HullSizingService.Data;
using HullSizingService.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace HullSizingService.Tests.Integration;

/// <summary>
/// Integration tests to verify seed data is properly populated in HullSizingService
/// CRITICAL: Empty hull families = zero candidates from first-principles solver
/// </summary>
public class SeedDataIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private ServiceProvider? _serviceProvider;
    private SizingDbContext? _context;

    public SeedDataIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<SizingDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddLogging();
        services.AddScoped<CsvDataSeeder>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<SizingDbContext>();

        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulateHullFamilies()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var families = await _context!.HullFamilyPresets.ToListAsync();

        Assert.NotEmpty(families);
        Assert.Equal(5, families.Count); // container, tanker, bulker, general_cargo, fishing

        Assert.Contains(families, f => f.Family == "container");
        Assert.Contains(families, f => f.Family == "tanker");
        Assert.Contains(families, f => f.Family == "bulker");
        Assert.Contains(families, f => f.Family == "general_cargo");
        Assert.Contains(families, f => f.Family == "fishing");

        _output.WriteLine($"✅ Hull Families: {families.Count} records seeded");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulateISOContainers()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var containers = await _context!.IsoContainers.ToListAsync();

        Assert.NotEmpty(containers);
        Assert.Equal(8, containers.Count); // 20', 40', 40'HC, 45', etc.

        // Verify standard containers exist
        Assert.Contains(containers, c => c.ContainerType.Contains("20"));
        Assert.Contains(containers, c => c.ContainerType.Contains("40"));

        _output.WriteLine($"✅ ISO Containers: {containers.Count} records seeded");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulateKPIWeights()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var weights = await _context!.KpiWeights.ToListAsync();

        Assert.NotEmpty(weights);
        Assert.Equal(5, weights.Count); // System default weights

        // Verify key metrics exist
        Assert.Contains(weights, w => w.Metric == "delta_balance");
        Assert.Contains(weights, w => w.Metric == "installed_power");
        Assert.Contains(weights, w => w.Metric == "constraints_ok");

        _output.WriteLine($"✅ KPI Weights: {weights.Count} records seeded");
    }

    [Fact]
    public async Task HullFamilies_ShouldHaveValidRatioRanges()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();
        await seeder.SeedAllAsync();

        // Act
        var families = await _context!.HullFamilyPresets.ToListAsync();

        // Assert - Verify ratio ranges are sensible
        foreach (var family in families)
        {
            Assert.True(family.LOverBMin > 0 && family.LOverBMin < family.LOverBMax,
                $"{family.Family}: Invalid L/B range");

            Assert.True(family.BOverTMin > 0 && family.BOverTMin < family.BOverTMax,
                $"{family.Family}: Invalid B/T range");

            Assert.True(family.CbMin > 0 && family.CbMin < family.CbMax && family.CbMax <= 1.0m,
                $"{family.Family}: Invalid Cb range");

            Assert.True(family.FnMin.HasValue && family.FnMax.HasValue &&
                       family.FnMin > 0 && family.FnMin < family.FnMax,
                $"{family.Family}: Invalid Froude number range");

            _output.WriteLine($"✅ {family.Family}: L/B={family.LOverBMin:F1}-{family.LOverBMax:F1}, " +
                            $"B/T={family.BOverTMin:F1}-{family.BOverTMax:F1}, " +
                            $"Cb={family.CbMin:F2}-{family.CbMax:F2}, " +
                            $"Fn={family.FnMin:F2}-{family.FnMax:F2}");
        }
    }

    [Fact]
    public async Task SeedAllAsync_ShouldNotDuplicateOnMultipleCalls()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();

        // Act
        await seeder.SeedAllAsync();
        var firstFamilyCount = await _context!.HullFamilyPresets.CountAsync();
        var firstContainerCount = await _context!.IsoContainers.CountAsync();
        var firstWeightCount = await _context!.KpiWeights.CountAsync();

        await seeder.SeedAllAsync();
        var secondFamilyCount = await _context!.HullFamilyPresets.CountAsync();
        var secondContainerCount = await _context!.IsoContainers.CountAsync();
        var secondWeightCount = await _context!.KpiWeights.CountAsync();

        // Assert
        Assert.Equal(firstFamilyCount, secondFamilyCount);
        Assert.Equal(firstContainerCount, secondContainerCount);
        Assert.Equal(firstWeightCount, secondWeightCount);

        _output.WriteLine($"✅ Idempotency: Counts unchanged after second run");
    }

    /// <summary>
    /// CRITICAL TEST: Verifies minimum requirements for first-principles solver to work
    /// If this fails, solver will generate ZERO candidates
    /// </summary>
    [Fact]
    public async Task SeedAllAsync_ShouldMeetMinimumRequirements_CRITICAL()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var familyCount = await _context!.HullFamilyPresets.Where(f => f.IsActive).CountAsync();
        var containerCount = await _context!.IsoContainers.CountAsync();
        var weightCount = await _context!.KpiWeights.CountAsync();

        var errors = new List<string>();

        if (familyCount < 5)
            errors.Add($"Hull families: Expected >= 5, got {familyCount} (BLOCKS FIRST-PRINCIPLES SOLVER!)");

        if (containerCount < 8)
            errors.Add($"ISO containers: Expected >= 8, got {containerCount}");

        if (weightCount < 5)
            errors.Add($"KPI weights: Expected >= 5, got {weightCount} (BLOCKS CANDIDATE SCORING!)");

        if (errors.Any())
        {
            var errorMessage = "❌ CRITICAL SEED DATA MISSING:\n  " + string.Join("\n  ", errors);
            _output.WriteLine(errorMessage);
            Assert.Fail(errorMessage);
        }

        _output.WriteLine("✅ CRITICAL: All minimum seed data requirements met");
        _output.WriteLine($"  - Hull Families (active): {familyCount}");
        _output.WriteLine($"  - ISO Containers: {containerCount}");
        _output.WriteLine($"  - KPI Weights: {weightCount}");
    }

    [Theory]
    [InlineData("container", 0.20, 0.28)] // Container ships: Fn 0.20-0.28
    [InlineData("tanker", 0.12, 0.18)]    // Tankers: Fn 0.12-0.18
    [InlineData("bulker", 0.14, 0.20)]    // Bulkers: Fn 0.14-0.20
    public async Task HullFamilies_ShouldHaveExpectedFroudeNumberRanges(string family, decimal expectedMinFn, decimal expectedMaxFn)
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();
        await seeder.SeedAllAsync();

        // Act
        var hullFamily = await _context!.HullFamilyPresets
            .FirstOrDefaultAsync(f => f.Family == family);

        // Assert
        Assert.NotNull(hullFamily);
        Assert.Equal(expectedMinFn, hullFamily.FnMin);
        Assert.Equal(expectedMaxFn, hullFamily.FnMax);

        _output.WriteLine($"✅ {family}: Fn range {hullFamily.FnMin:F2}-{hullFamily.FnMax:F2}");
    }

    [Fact]
    public async Task KPIWeights_ShouldSumToReasonableValue()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CsvDataSeeder>();
        await seeder.SeedAllAsync();

        // Act
        var weights = await _context!.KpiWeights.ToListAsync();
        var totalWeight = weights.Sum(w => w.Weight);

        // Assert - Sum should be close to 1.0 (100%)
        Assert.InRange(totalWeight, 0.95m, 1.05m);

        _output.WriteLine($"✅ KPI Weights: Total = {totalWeight:F2} (within range 0.95-1.05)");
        foreach (var weight in weights.OrderByDescending(w => w.Weight))
        {
            _output.WriteLine($"  - {weight.Metric}: {weight.Weight:F2} ({weight.Weight * 100:F0}%)");
        }
    }
}
