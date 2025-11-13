using DataService.Data;
using DataService.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using XunitLogger;

namespace DataService.Tests.Integration;

/// <summary>
/// Integration tests to verify seed data is properly populated
/// These tests prevent the "empty catalog" issue from recurring
/// </summary>
public class SeedDataIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private ServiceProvider? _serviceProvider;
    private DataDbContext? _context;

    public SeedDataIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection();

        services.AddDbContext<DataDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddLogging();
        services.AddScoped<CatalogSeeder>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<DataDbContext>();

        // Ensure database is created
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
    public async Task SeedAllAsync_ShouldPopulateWaterProperties()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var waterProps = await _context!.CatalogWaterProperties.ToListAsync();

        Assert.NotEmpty(waterProps);
        Assert.Equal(6, waterProps.Count); // 3 Fresh + 3 Sea

        // Verify Fresh water at 0°C, 15°C, 30°C
        Assert.Contains(waterProps, w => w.Medium == "Fresh" && w.Temperature_C == 0);
        Assert.Contains(waterProps, w => w.Medium == "Fresh" && w.Temperature_C == 15);
        Assert.Contains(waterProps, w => w.Medium == "Fresh" && w.Temperature_C == 30);

        // Verify Sea water at 0°C, 15°C, 30°C
        Assert.Contains(waterProps, w => w.Medium == "Sea" && w.Temperature_C == 0);
        Assert.Contains(waterProps, w => w.Medium == "Sea" && w.Temperature_C == 15);
        Assert.Contains(waterProps, w => w.Medium == "Sea" && w.Temperature_C == 30);

        _output.WriteLine($"✅ Water Properties: {waterProps.Count} records seeded");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulatePropellerSeries()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var propellers = await _context!.CatalogPropellerSeries
            .Include(p => p.OpenWaterPoints)
            .ToListAsync();

        Assert.NotEmpty(propellers);
        Assert.True(propellers.Count >= 1); // At least Wageningen placeholder

        var wageningen = propellers.FirstOrDefault(p => p.Name.Contains("Wageningen"));
        Assert.NotNull(wageningen);
        Assert.NotEmpty(wageningen.OpenWaterPoints);

        _output.WriteLine($"✅ Propeller Series: {propellers.Count} records seeded");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulateTemplateHulls()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var templates = await _context!.BenchmarkCases
            .Where(b => b.HullType == "Template")
            .ToListAsync();

        Assert.NotEmpty(templates);
        Assert.Equal(3, templates.Count); // Wigley, Series60, Prismatic

        Assert.Contains(templates, t => t.Slug == "wigley-hull");
        Assert.Contains(templates, t => t.Slug == "series60-like");
        Assert.Contains(templates, t => t.Slug == "prismatic-npc");

        _output.WriteLine($"✅ Template Hulls: {templates.Count} records seeded");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulateWigleyGeometry()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var wigley = await _context!.BenchmarkCases
            .Include(c => c.Geometries)
            .FirstOrDefaultAsync(c => c.Slug == "wigley-hull");

        Assert.NotNull(wigley);
        Assert.NotEmpty(wigley.Geometries);
        Assert.False(wigley.GeometryMissing);

        var geometry = wigley.Geometries.First();
        Assert.NotNull(geometry.StationsJson);
        Assert.NotNull(geometry.WaterlinesJson);
        Assert.NotNull(geometry.OffsetsJson);

        _output.WriteLine($"✅ Wigley Geometry: Complete with stations, waterlines, offsets");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldPopulateBenchmarkHulls()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var benchmarks = await _context!.BenchmarkCases
            .Where(b => b.HullType != null && b.HullType != "Template")
            .ToListAsync();

        Assert.NotEmpty(benchmarks);
        Assert.Equal(3, benchmarks.Count); // KCS, KVLCC2, DTMB-5415

        Assert.Contains(benchmarks, b => b.Slug == "kcs");
        Assert.Contains(benchmarks, b => b.Slug == "kvlcc2");
        Assert.Contains(benchmarks, b => b.Slug == "dtmb-5415");

        // Verify they have principal particulars
        var kcs = benchmarks.First(b => b.Slug == "kcs");
        Assert.NotNull(kcs.Lpp_m);
        Assert.NotNull(kcs.B_m);
        Assert.NotNull(kcs.T_m);
        Assert.NotNull(kcs.Cb);

        _output.WriteLine($"✅ Benchmark Hulls: {benchmarks.Count} records seeded");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldNotDuplicateOnMultipleCalls()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act - Run seeder twice
        await seeder.SeedAllAsync();
        var firstCount = await _context!.BenchmarkCases.CountAsync();

        await seeder.SeedAllAsync();
        var secondCount = await _context!.BenchmarkCases.CountAsync();

        // Assert
        Assert.Equal(firstCount, secondCount);
        _output.WriteLine($"✅ Idempotency Check: Count remained {firstCount} after second run");
    }

    [Fact]
    public async Task SeedAllAsync_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await seeder.SeedAllAsync();
        stopwatch.Stop();

        // Assert - Should complete in under 5 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Seeding took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

        _output.WriteLine($"✅ Performance: Seeding completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Theory]
    [InlineData("Fresh", 0)]
    [InlineData("Fresh", 15)]
    [InlineData("Fresh", 30)]
    [InlineData("Sea", 0)]
    [InlineData("Sea", 15)]
    [InlineData("Sea", 30)]
    public async Task SeedAllAsync_ShouldHaveWaterPropertyForEachMediumAndTemp(string medium, decimal temp)
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert
        var waterProp = await _context!.CatalogWaterProperties
            .FirstOrDefaultAsync(w => w.Medium == medium && w.Temperature_C == temp);

        Assert.NotNull(waterProp);
        Assert.True(waterProp.Density_kgm3 > 0);
        Assert.True(waterProp.KinematicViscosity_m2s > 0);
        Assert.NotNull(waterProp.SourceRef);

        _output.WriteLine($"✅ Water Property: {medium} @ {temp}°C - Density={waterProp.Density_kgm3:F2} kg/m³");
    }

    /// <summary>
    /// CRITICAL TEST: Ensures all minimum required data is present
    /// This is what should be checked in production health checks
    /// </summary>
    [Fact]
    public async Task SeedAllAsync_ShouldMeetMinimumRequirements_CRITICAL()
    {
        // Arrange
        var seeder = _serviceProvider!.GetRequiredService<CatalogSeeder>();

        // Act
        await seeder.SeedAllAsync();

        // Assert - MINIMUM REQUIREMENTS
        var waterCount = await _context!.CatalogWaterProperties.CountAsync();
        var propellerCount = await _context!.CatalogPropellerSeries.CountAsync();
        var benchmarkCount = await _context!.BenchmarkCases.CountAsync();
        var wigleyGeometry = await _context!.BenchmarkGeometries
            .AnyAsync(g => g.Case != null && g.Case.Slug == "wigley-hull");

        var errors = new List<string>();

        if (waterCount < 6)
            errors.Add($"Water properties: Expected >= 6, got {waterCount}");

        if (propellerCount < 1)
            errors.Add($"Propeller series: Expected >= 1, got {propellerCount}");

        if (benchmarkCount < 6)
            errors.Add($"Benchmark cases: Expected >= 6 (3 templates + 3 benchmarks), got {benchmarkCount}");

        if (!wigleyGeometry)
            errors.Add("Wigley hull geometry is missing");

        if (errors.Any())
        {
            var errorMessage = "❌ CRITICAL SEED DATA MISSING:\n  " + string.Join("\n  ", errors);
            _output.WriteLine(errorMessage);
            Assert.Fail(errorMessage);
        }

        _output.WriteLine("✅ CRITICAL: All minimum seed data requirements met");
        _output.WriteLine($"  - Water Properties: {waterCount}");
        _output.WriteLine($"  - Propeller Series: {propellerCount}");
        _output.WriteLine($"  - Benchmark Cases: {benchmarkCount}");
        _output.WriteLine($"  - Wigley Geometry: Present");
    }
}
