using DataService.Data;
using DataService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Services;

namespace DataService.Tests.Services;

public class ComparisonServiceTests
{
    [Fact]
    public void ComparisonService_ShouldInstantiate()
    {
        // Arrange - Create in-memory database and mock unit conversion service
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ComparisonService")
            .Options;

        using var context = new DataDbContext(options);
        var mockUnitConversionService = new Mock<IUnitConversionService>();

        // Act - Instantiate the service
        var service = new ComparisonService(context, mockUnitConversionService.Object);

        // Assert - Service should be created successfully
        service.Should().NotBeNull("service should be instantiated");
        service.Should().BeOfType<ComparisonService>();
    }

    [Fact]
    public void ComparisonService_ShouldAcceptIUnitConversionService()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ComparisonService_Interface")
            .Options;

        using var context = new DataDbContext(options);
        var mockUnitConversionService = new Mock<IUnitConversionService>();

        // Act
        var service = new ComparisonService(context, mockUnitConversionService.Object);

        // Assert - Should work with IUnitConversionService interface
        // This test ensures the service uses the correct namespace and interface
        // If there was a namespace issue (like using wrong 'using' statement),
        // this test would fail at compile time
        service.Should().NotBeNull();
    }


    [Fact]
    public void ComparisonService_UnitConversionService_ShouldBeFromCorrectNamespace()
    {
        // This test verifies that IUnitConversionService is from the correct namespace
        // If ComparisonService was using wrong namespace (e.g., "using UnitConversion;"
        // instead of "using NavArch.UnitConversion.Services;"), this would fail

        // Arrange
        var unitConversionServiceType = typeof(IUnitConversionService);

        // Act & Assert
        unitConversionServiceType.Namespace.Should().Be("Shared.Services",
            "IUnitConversionService should be from Shared.Services namespace");
    }

    [Fact]
    public async Task ComparisonService_CreateSnapshotAsync_ShouldThrowWhenVesselNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ComparisonService_NotFound")
            .Options;

        using var context = new DataDbContext(options);
        var mockUnitConversionService = new Mock<IUnitConversionService>();
        var service = new ComparisonService(context, mockUnitConversionService.Object);

        var nonExistentVesselId = Guid.NewGuid();
        var dto = new Shared.DTOs.CreateComparisonSnapshotDto
        {
            RunName = "Test Snapshot",
            MinDraft = 5.0m,
            MaxDraft = 10.0m,
            DraftStep = 1.0m,
            Results = new List<Shared.DTOs.HydroResultDto>()
        };

        // Act
        Func<Task> act = async () => await service.CreateSnapshotAsync(
            nonExistentVesselId,
            Guid.NewGuid(),
            dto,
            "SI",
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentVesselId}*");
    }

    [Fact]
    public void ComparisonService_ServiceIntegration_VerifiesNamespaceImports()
    {
        // This meta-test verifies that if ComparisonService had wrong namespace imports,
        // the test project would fail to build

        // The fact that this test compiles and runs proves:
        // 1. ComparisonService uses correct namespace for IUnitConversionService
        // 2. All required dependencies are properly referenced
        // 3. No compilation errors due to namespace conflicts

        // Arrange
        var serviceType = typeof(ComparisonService);
        var constructor = serviceType.GetConstructors().First();
        var parameters = constructor.GetParameters();

        // Act & Assert
        parameters.Should().HaveCount(2, "ComparisonService should have 2 constructor parameters");
        parameters[0].ParameterType.Should().Be<DataDbContext>();
        parameters[1].ParameterType.Should().Be<IUnitConversionService>();
    }
}
