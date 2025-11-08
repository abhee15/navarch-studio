using DataService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataService.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces PostgreSQL with in-memory database for integration tests
/// This allows tests to run without requiring a real database connection
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DataDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<DataDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestDb");
            });

            // Build the service provider and seed test data
            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<DataDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();

            // Seed test data if needed
            SeedTestData(db);
        });
    }

    private static void SeedTestData(DataDbContext db)
    {
        // Add any test data needed for integration tests
        // For now, we'll let the application's built-in seeding handle it
        // The catalog seeder and other seeders will run during app startup
    }
}
