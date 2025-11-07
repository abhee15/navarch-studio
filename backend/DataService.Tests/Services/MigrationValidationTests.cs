using System.Reflection;
using System.Text.RegularExpressions;
using DataService.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataService.Tests.Services;

public class MigrationValidationTests
{
    [Fact]
    public void AllMigrations_ShouldNotBeEmpty()
    {
        // Arrange - Find all migration classes
        var assembly = typeof(DataDbContext).Assembly;
        var migrationTypes = assembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        migrationTypes.Should().NotBeEmpty("should have at least one migration");

        // Act & Assert - Check each migration
        foreach (var migrationType in migrationTypes)
        {
            var migration = (Migration)Activator.CreateInstance(migrationType)!;
            var upMethod = migrationType.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var downMethod = migrationType.GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            upMethod.Should().NotBeNull($"{migrationType.Name} should have Up method");
            downMethod.Should().NotBeNull($"{migrationType.Name} should have Down method");

            // Check if methods have actual operations (not empty)
            var upMethodBody = upMethod!.GetMethodBody();
            var downMethodBody = downMethod!.GetMethodBody();

            // Method body should have more than just a return statement
            // Empty methods typically have IL size < 10 bytes
            upMethodBody.Should().NotBeNull($"{migrationType.Name}.Up should have a method body");
            upMethodBody!.GetILAsByteArray()!.Length.Should().BeGreaterThan(10,
                $"{migrationType.Name}.Up method appears to be empty (no migration operations)");
        }
    }

    [Fact]
    public void InitialCreate_ShouldIncludeAllDbSetTables()
    {
        // Arrange - Get all DbSet properties from DataDbContext
        var dbContextType = typeof(DataDbContext);
        var dbSetProperties = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        dbSetProperties.Should().NotBeEmpty("DataDbContext should have DbSet properties");

        // Get table names from DbSet properties (use snake_case convention)
        var expectedTableNames = dbSetProperties
            .Select(p => ToSnakeCase(GetPluralName(p.Name)))
            .ToList();

        // Read InitialCreate migration file
        var migrationFile = FindInitialCreateMigrationFile();
        migrationFile.Should().NotBeNull("InitialCreate migration file should exist");

        var migrationContent = File.ReadAllText(migrationFile!);

        // Act & Assert - Check each expected table
        foreach (var tableName in expectedTableNames)
        {
            var createTablePattern = $@"CreateTable\s*\(\s*name:\s*""{tableName}""";
            migrationContent.Should().MatchRegex(createTablePattern,
                $"InitialCreate migration should create table '{tableName}' (from DbSet<>.{PascalToReadable(tableName)})");
        }
    }

    [Theory]
    [InlineData("vessels", "data")]
    [InlineData("parametric_hulls", "catalog_ml")]
    [InlineData("catalog_real_hulls", "catalog_real")]
    [InlineData("benchmark_case", "data")]
    [InlineData("project_boards", "data")]
    [InlineData("catalog_propeller_series", "data")]
    public void InitialCreate_ShouldIncludeTable(string tableName, string schemaName)
    {
        // Arrange
        var migrationFile = FindInitialCreateMigrationFile();
        migrationFile.Should().NotBeNull($"InitialCreate migration file should exist");

        var migrationContent = File.ReadAllText(migrationFile!);

        // Act & Assert
        var createTablePattern = $@"CreateTable\s*\(\s*name:\s*""{tableName}""\s*,\s*schema:\s*""{schemaName}""";
        migrationContent.Should().MatchRegex(createTablePattern,
            $"InitialCreate migration should create table '{tableName}' in schema '{schemaName}'");
    }

    [Fact]
    public void InitialCreate_ShouldNotBeEmpty()
    {
        // Arrange
        var migrationFile = FindInitialCreateMigrationFile();
        migrationFile.Should().NotBeNull("InitialCreate migration file should exist");

        var migrationContent = File.ReadAllText(migrationFile!);

        // Act & Assert
        migrationContent.Should().Contain("CreateTable",
            "InitialCreate migration should contain CreateTable operations");
        migrationContent.Should().Contain("protected override void Up",
            "InitialCreate migration should have Up method");
        migrationContent.Should().Contain("protected override void Down",
            "InitialCreate migration should have Down method");

        // Should have multiple CreateTable calls (we have 30+ tables)
        var createTableCount = Regex.Matches(migrationContent, @"CreateTable\s*\(").Count;
        createTableCount.Should().BeGreaterThan(10,
            "InitialCreate migration should create multiple tables");
    }

    [Fact]
    public void AllMigrationFiles_ShouldHaveDesignerFiles()
    {
        // Arrange - Find all .cs migration files
        var migrationsDir = FindMigrationsDirectory();
        migrationsDir.Should().NotBeNull("Migrations directory should exist");

        var migrationFiles = Directory.GetFiles(migrationsDir!, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs") && !f.EndsWith("ModelSnapshot.cs"))
            .ToList();

        migrationFiles.Should().NotBeEmpty("should have at least one migration file");

        // Act & Assert - Each migration should have a corresponding .Designer.cs file
        foreach (var migrationFile in migrationFiles)
        {
            var designerFile = migrationFile.Replace(".cs", ".Designer.cs");
            File.Exists(designerFile).Should().BeTrue(
                $"Migration {Path.GetFileName(migrationFile)} should have a corresponding Designer file");
        }
    }

    [Fact]
    public void ModelSnapshot_ShouldExist()
    {
        // Arrange
        var migrationsDir = FindMigrationsDirectory();
        migrationsDir.Should().NotBeNull("Migrations directory should exist");

        // Act
        var snapshotFile = Directory.GetFiles(migrationsDir!, "*ModelSnapshot.cs").FirstOrDefault();

        // Assert
        snapshotFile.Should().NotBeNull("DataDbContextModelSnapshot.cs should exist");

        var snapshotContent = File.ReadAllText(snapshotFile!);
        snapshotContent.Should().Contain("class DataDbContextModelSnapshot",
            "Model snapshot should contain proper class name");
    }

    // Helper methods
    private string? FindInitialCreateMigrationFile()
    {
        var migrationsDir = FindMigrationsDirectory();
        if (migrationsDir == null) return null;

        return Directory.GetFiles(migrationsDir, "*_InitialCreate.cs")
            .FirstOrDefault(f => !f.EndsWith(".Designer.cs"));
    }

    private string? FindMigrationsDirectory()
    {
        // Start from assembly location and search up for Migrations directory
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var currentDir = assemblyPath;

        while (currentDir != null)
        {
            // Go up to find the backend directory
            var backendDir = Path.Combine(currentDir, "backend");
            if (Directory.Exists(backendDir))
            {
                var migrationsDir = Path.Combine(backendDir, "DataService", "Migrations");
                if (Directory.Exists(migrationsDir))
                {
                    return migrationsDir;
                }
            }

            // Also check relative paths from test project
            var dataServiceDir = Path.Combine(currentDir, "..", "..", "..", "..", "DataService", "Migrations");
            if (Directory.Exists(dataServiceDir))
            {
                return Path.GetFullPath(dataServiceDir);
            }

            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        // Fallback: Try relative path from test location
        var testLocation = AppContext.BaseDirectory;
        var relativePath = Path.Combine(testLocation, "..", "..", "..", "..", "DataService", "Migrations");
        if (Directory.Exists(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        return null;
    }

    private string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return Regex.Replace(text, @"([a-z])([A-Z])", "$1_$2").ToLower();
    }

    private string GetPluralName(string singular)
    {
        // Simple pluralization (works for most cases)
        // DbSet property names are usually already plural
        return singular;
    }

    private string PascalToReadable(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    }
}
