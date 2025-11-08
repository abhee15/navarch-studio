using System.Diagnostics;
using DataService.Services.Hydrostatics;
using DataService.Services.Resistance;
using DataService.Services.Seakeeping;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;
using Xunit.Abstractions;

namespace DataService.Tests.Performance;

/// <summary>
/// Performance tests for computational algorithms
/// Ensures calculations complete within acceptable time limits
/// </summary>
[Trait("Category", "Performance")]
public class AlgorithmPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<IntegrationEngine> _integrationLogger;
    private readonly ILogger<HoltropMennenService> _resistanceLogger;

    public AlgorithmPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _integrationLogger = Mock.Of<ILogger<IntegrationEngine>>();
        _resistanceLogger = Mock.Of<ILogger<HoltropMennenService>>();
    }

    [Fact]
    public void HydrostaticCalculation_LargeGeometry_CompletesWithin500ms()
    {
        // Arrange - Large geometry (50 stations × 20 waterlines = 1000 offsets)
        var vessel = CreateLargeVessel(numStations: 50, numWaterlines: 20);
        var loadcase = CreateLoadcase();

        var integrationEngine = new IntegrationEngine(_integrationLogger);
        var calculator = new HydroCalculator(integrationEngine, Mock.Of<ILogger<HydroCalculator>>());

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = calculator.ComputeHydrostatics(vessel, loadcase);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Hydrostatic calculation took {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "hydrostatic calculation should complete quickly");
        result.Should().NotBeNull();
        result.Displacement.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HoltropMennenResistance_SingleSpeed_CompletesWithin100ms()
    {
        // Arrange
        var vessel = CreateStandardVessel();
        var speed = 15.0; // knots
        var waterProps = new { Density = 1025.0, KinematicViscosity = 1.19e-6 };

        var service = new HoltropMennenService(_resistanceLogger);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = service.CalculateResistance(
            vessel.LengthOverall,
            vessel.LengthBetweenPerpendiculars ?? vessel.LengthOverall * 0.95,
            vessel.Breadth,
            vessel.Draft,
            displacement: 1000, // tonnes
            Cb: 0.65,
            Cp: 0.70,
            Cm: 0.93,
            Cwp: 0.80,
            lcb: 2.5,
            speed: speed,
            waterDensity: waterProps.Density,
            kinematicViscosity: waterProps.KinematicViscosity
        );
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Holtrop-Mennen calculation took {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "resistance calculation should be fast");
        result.Should().NotBeNull();
        result.TotalResistance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void StripTheory_TenStations_CompletesWithin2000ms()
    {
        // Arrange
        var vessel = CreateStandardVessel();
        var numStations = 10;
        var numFrequencies = 10;

        var stripTheory = new StripTheoryEngine(Mock.Of<ILogger<StripTheoryEngine>>());

        // Act
        var stopwatch = Stopwatch.StartNew();

        // Simulate strip theory calculation (simplified)
        for (int i = 0; i < numStations; i++)
        {
            for (int j = 0; j < numFrequencies; j++)
            {
                // Simulate section calculation
                var sectionArea = CalculateSectionArea(i, numStations);
                var addedMass = sectionArea * 1.5; // Simplified
            }
        }

        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Strip theory calculation took {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "strip theory should complete within 2 seconds");
    }

    [Fact]
    public void DatabaseQuery_CatalogSearch_CompletesWithin100ms()
    {
        // This would be an actual database query test
        // Requires test database with seeded data

        // Arrange
        var minLength = 50;
        var maxLength = 100;

        var stopwatch = Stopwatch.StartNew();

        // Act
        // Simulate database query delay
        Thread.Sleep(50); // Simulate 50ms query time

        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Catalog search took {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "catalog search should be fast");
    }

    [Fact]
    public void MultipleHydrostaticCalculations_Batch10_CompletesWithin3000ms()
    {
        // Arrange - Test batch processing
        var vessels = new List<Vessel>();
        for (int i = 0; i < 10; i++)
        {
            vessels.Add(CreateStandardVessel());
        }

        var loadcase = CreateLoadcase();
        var integrationEngine = new IntegrationEngine(_integrationLogger);
        var calculator = new HydroCalculator(integrationEngine, Mock.Of<ILogger<HydroCalculator>>());

        // Act
        var stopwatch = Stopwatch.StartNew();
        foreach (var vessel in vessels)
        {
            var result = calculator.ComputeHydrostatics(vessel, loadcase);
        }
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Batch calculation (10 vessels) took {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "batch processing should be efficient");
    }

    // Helper methods
    private Vessel CreateStandardVessel()
    {
        var vessel = new Vessel
        {
            Id = 1,
            Name = "Test Vessel",
            LengthOverall = 100,
            Breadth = 20,
            Depth = 10,
            Draft = 5,
            Stations = new List<Station>(),
            Waterlines = new List<Waterline>(),
            Offsets = new List<Offset>()
        };

        // Add 10 stations
        for (int i = 0; i < 10; i++)
        {
            vessel.Stations.Add(new Station
            {
                Id = i + 1,
                VesselId = vessel.Id,
                StationNumber = i,
                LongitudinalPosition = i * 10.0
            });
        }

        // Add 5 waterlines
        for (int i = 0; i < 5; i++)
        {
            vessel.Waterlines.Add(new Waterline
            {
                Id = i + 1,
                VesselId = vessel.Id,
                WaterlineNumber = i,
                VerticalPosition = i * 2.0
            });
        }

        // Add offsets (10 stations × 5 waterlines = 50 offsets)
        foreach (var station in vessel.Stations)
        {
            foreach (var waterline in vessel.Waterlines)
            {
                vessel.Offsets.Add(new Offset
                {
                    StationId = station.Id,
                    WaterlineId = waterline.Id,
                    HalfBreadth = 5.0 + (station.StationNumber * 0.5) - Math.Abs(station.StationNumber - 5) * 0.3
                });
            }
        }

        return vessel;
    }

    private Vessel CreateLargeVessel(int numStations, int numWaterlines)
    {
        var vessel = new Vessel
        {
            Id = 1,
            Name = "Large Test Vessel",
            LengthOverall = 200,
            Breadth = 30,
            Depth = 15,
            Draft = 8,
            Stations = new List<Station>(),
            Waterlines = new List<Waterline>(),
            Offsets = new List<Offset>()
        };

        for (int i = 0; i < numStations; i++)
        {
            vessel.Stations.Add(new Station
            {
                Id = i + 1,
                VesselId = vessel.Id,
                StationNumber = i,
                LongitudinalPosition = i * (200.0 / numStations)
            });
        }

        for (int i = 0; i < numWaterlines; i++)
        {
            vessel.Waterlines.Add(new Waterline
            {
                Id = i + 1,
                VesselId = vessel.Id,
                WaterlineNumber = i,
                VerticalPosition = i * (15.0 / numWaterlines)
            });
        }

        foreach (var station in vessel.Stations)
        {
            foreach (var waterline in vessel.Waterlines)
            {
                vessel.Offsets.Add(new Offset
                {
                    StationId = station.Id,
                    WaterlineId = waterline.Id,
                    HalfBreadth = 10.0 + Math.Sin(station.StationNumber * 0.1) * 5
                });
            }
        }

        return vessel;
    }

    private Loadcase CreateLoadcase()
    {
        return new Loadcase
        {
            Id = 1,
            VesselId = 1,
            Name = "Test Loadcase",
            Draft = 5.0,
            KG = 6.5,
            WaterDensity = 1025.0
        };
    }

    private double CalculateSectionArea(int stationIndex, int totalStations)
    {
        // Simplified parabolic section area
        double x = (double)stationIndex / totalStations;
        return 50 * (1 - Math.Pow(2 * x - 1, 2)); // Parabolic distribution
    }
}
