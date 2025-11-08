using Shared.Models;

namespace DataService.Tests.Helpers;

/// <summary>
/// Test data generator for creating realistic test vessels, loadcases, and geometry
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Create a rectangular barge with known analytical properties
    /// </summary>
    public static Vessel CreateRectangularBarge()
    {
        var vessel = new Vessel
        {
            Id = 1,
            Name = "Rectangular Barge",
            LengthOverall = 100.0,
            LengthBetweenPerpendiculars = 100.0,
            Breadth = 20.0,
            Depth = 10.0,
            Draft = 5.0,
            Stations = new List<Station>(),
            Waterlines = new List<Waterline>(),
            Offsets = new List<Offset>()
        };

        // Create 11 stations (0-10)
        for (int i = 0; i <= 10; i++)
        {
            vessel.Stations.Add(new Station
            {
                Id = i + 1,
                VesselId = vessel.Id,
                StationNumber = i,
                LongitudinalPosition = i * 10.0
            });
        }

        // Create 6 waterlines (0-5m)
        for (int i = 0; i <= 5; i++)
        {
            vessel.Waterlines.Add(new Waterline
            {
                Id = i + 1,
                VesselId = vessel.Id,
                WaterlineNumber = i,
                VerticalPosition = i * 1.0
            });
        }

        // All offsets are constant (half-breadth = 10m) for rectangular barge
        foreach (var station in vessel.Stations)
        {
            foreach (var waterline in vessel.Waterlines)
            {
                vessel.Offsets.Add(new Offset
                {
                    StationId = station.Id,
                    WaterlineId = waterline.Id,
                    HalfBreadth = 10.0 // Constant for rectangular barge
                });
            }
        }

        return vessel;
    }

    /// <summary>
    /// Create a Wigley hull with parabolic waterlines (analytical benchmark)
    /// </summary>
    public static Vessel CreateWigleyHull()
    {
        var vessel = new Vessel
        {
            Id = 2,
            Name = "Wigley Hull",
            LengthOverall = 3.0,
            LengthBetweenPerpendiculars = 3.0,
            Breadth = 0.3,
            Depth = 0.1875,
            Draft = 0.0625,
            Stations = new List<Station>(),
            Waterlines = new List<Waterline>(),
            Offsets = new List<Offset>()
        };

        // Create 21 stations (0-20)
        for (int i = 0; i <= 20; i++)
        {
            vessel.Stations.Add(new Station
            {
                Id = i + 1,
                VesselId = vessel.Id,
                StationNumber = i,
                LongitudinalPosition = i * 0.15 // 3m / 20 = 0.15m spacing
            });
        }

        // Create 7 waterlines
        for (int i = 0; i <= 6; i++)
        {
            vessel.Waterlines.Add(new Waterline
            {
                Id = i + 1,
                VesselId = vessel.Id,
                WaterlineNumber = i,
                VerticalPosition = i * 0.03125 // 0.1875m / 6
            });
        }

        // Wigley parabolic form: y = (B/2) * (1 - (z/D)^2) * (1 - (x/(L/2))^2)
        var L = vessel.LengthOverall;
        var B = vessel.Breadth;
        var D = vessel.Depth;

        foreach (var station in vessel.Stations)
        {
            var x = station.LongitudinalPosition - L / 2; // Shift to centered coordinates
            foreach (var waterline in vessel.Waterlines)
            {
                var z = waterline.VerticalPosition;
                var halfBreadth = (B / 2) * (1 - Math.Pow(z / D, 2)) * (1 - Math.Pow(x / (L / 2), 2));

                vessel.Offsets.Add(new Offset
                {
                    StationId = station.Id,
                    WaterlineId = waterline.Id,
                    HalfBreadth = Math.Max(0, halfBreadth)
                });
            }
        }

        return vessel;
    }

    /// <summary>
    /// Create a cargo ship hull (representative of Series 60)
    /// </summary>
    public static Vessel CreateCargoShip()
    {
        var vessel = new Vessel
        {
            Id = 3,
            Name = "Cargo Ship (Series 60)",
            LengthOverall = 150.0,
            LengthBetweenPerpendiculars = 142.5,
            Breadth = 21.5,
            Depth = 12.5,
            Draft = 8.5,
            Stations = new List<Station>(),
            Waterlines = new List<Waterline>(),
            Offsets = new List<Offset>()
        };

        // Create 21 stations (standard for ship design)
        for (int i = 0; i <= 20; i++)
        {
            vessel.Stations.Add(new Station
            {
                Id = i + 1,
                VesselId = vessel.Id,
                StationNumber = i,
                LongitudinalPosition = i * (142.5 / 20)
            });
        }

        // Create 10 waterlines
        for (int i = 0; i <= 9; i++)
        {
            vessel.Waterlines.Add(new Waterline
            {
                Id = i + 1,
                VesselId = vessel.Id,
                WaterlineNumber = i,
                VerticalPosition = i * (12.5 / 9)
            });
        }

        // Approximate Series 60 hull form (Cb = 0.60)
        var L = vessel.LengthBetweenPerpendiculars.Value;
        var B = vessel.Breadth;
        var D = vessel.Depth;

        foreach (var station in vessel.Stations)
        {
            var xNorm = station.StationNumber / 20.0; // 0 at aft, 1 at forward
            foreach (var waterline in vessel.Waterlines)
            {
                var zNorm = waterline.VerticalPosition / D;

                // Approximate Series 60 section shape
                double halfBreadth;
                if (xNorm < 0.1) // Aft
                {
                    halfBreadth = (B / 2) * Math.Pow(xNorm / 0.1, 2) * (1 - Math.Pow(zNorm, 1.5));
                }
                else if (xNorm > 0.9) // Forward
                {
                    halfBreadth = (B / 2) * Math.Pow((1 - xNorm) / 0.1, 1.5) * (1 - Math.Pow(zNorm, 2));
                }
                else // Parallel middle body
                {
                    halfBreadth = (B / 2) * (1 - Math.Pow(zNorm, 1.2));
                }

                vessel.Offsets.Add(new Offset
                {
                    StationId = station.Id,
                    WaterlineId = waterline.Id,
                    HalfBreadth = Math.Max(0, halfBreadth)
                });
            }
        }

        return vessel;
    }

    /// <summary>
    /// Create a standard loadcase
    /// </summary>
    public static Loadcase CreateLoadcase(int vesselId = 1, string name = "Design Loadcase")
    {
        return new Loadcase
        {
            Id = 1,
            VesselId = vesselId,
            Name = name,
            Draft = 5.0,
            Trim = 0.0,
            KG = 6.5,
            WaterDensity = 1025.0,
            Description = "Standard design condition"
        };
    }

    /// <summary>
    /// Create multiple test vessels for batch testing
    /// </summary>
    public static List<Vessel> CreateTestFleet(int count = 5)
    {
        var fleet = new List<Vessel>
        {
            CreateRectangularBarge(),
            CreateWigleyHull(),
            CreateCargoShip()
        };

        // Add random vessels
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 3; i < count; i++)
        {
            var vessel = new Vessel
            {
                Id = i + 1,
                Name = $"Test Vessel {i + 1}",
                LengthOverall = 50 + random.Next(150),
                Breadth = 10 + random.Next(20),
                Depth = 5 + random.Next(10),
                Draft = 3 + random.Next(7),
                Stations = new List<Station>(),
                Waterlines = new List<Waterline>(),
                Offsets = new List<Offset>()
            };

            // Add basic geometry
            for (int j = 0; j <= 10; j++)
            {
                vessel.Stations.Add(new Station
                {
                    Id = j + 1,
                    VesselId = vessel.Id,
                    StationNumber = j,
                    LongitudinalPosition = j * (vessel.LengthOverall / 10)
                });
            }

            for (int j = 0; j <= 5; j++)
            {
                vessel.Waterlines.Add(new Waterline
                {
                    Id = j + 1,
                    VesselId = vessel.Id,
                    WaterlineNumber = j,
                    VerticalPosition = j * (vessel.Depth / 5)
                });
            }

            // Simple parabolic offsets
            foreach (var station in vessel.Stations)
            {
                var xNorm = station.StationNumber / 10.0;
                foreach (var waterline in vessel.Waterlines)
                {
                    var zNorm = waterline.VerticalPosition / vessel.Depth;
                    var halfBreadth = (vessel.Breadth / 2) * (1 - Math.Pow(zNorm, 2)) * (1 - Math.Pow(2 * xNorm - 1, 2));

                    vessel.Offsets.Add(new Offset
                    {
                        StationId = station.Id,
                        WaterlineId = waterline.Id,
                        HalfBreadth = Math.Max(0, halfBreadth)
                    });
                }
            }

            fleet.Add(vessel);
        }

        return fleet;
    }

    /// <summary>
    /// Create test data for resistance calculations
    /// </summary>
    public static (double Lpp, double B, double T, double Displacement, double Cb, double[] Speeds) CreateResistanceTestData()
    {
        return (
            Lpp: 150.0,
            B: 21.5,
            T: 8.5,
            Displacement: 15000.0,
            Cb: 0.65,
            Speeds: new[] { 10.0, 12.0, 14.0, 16.0, 18.0, 20.0 }
        );
    }
}
