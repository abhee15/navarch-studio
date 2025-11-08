using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.Extensions.Logging;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Strip theory implementation for computing hydrodynamic coefficients.
/// Phase 1: Simplified elliptic formulas for cross-sections.
/// Phase 5: Full Lewis conformal transform for accuracy.
/// </summary>
public class StripTheoryEngine : IStripTheoryEngine
{
    private readonly DataDbContext _context;
    private readonly IGeometryService _geometryService;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly ILogger<StripTheoryEngine> _logger;

    public StripTheoryEngine(
        DataDbContext context,
        IGeometryService geometryService,
        IIntegrationEngine integrationEngine,
        ILogger<StripTheoryEngine> logger)
    {
        _context = context;
        _geometryService = geometryService;
        _integrationEngine = integrationEngine;
        _logger = logger;
    }

    public async Task<HydrodynamicCoefficients> ComputeCoefficientsAsync(
        Guid vesselId,
        double draft,
        double[] frequencyRange,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Computing hydrodynamic coefficients for vessel {VesselId} at draft {Draft}m, {FreqCount} frequencies",
            vesselId, draft, frequencyRange.Length
        );

        // Get vessel geometry
        var offsetsGrid = await _geometryService.GetOffsetsGridAsync(vesselId, cancellationToken);

        if (offsetsGrid.Offsets.Count == 0)
        {
            throw new InvalidOperationException("No offsets data available for vessel");
        }

        var numFreq = frequencyRange.Length;
        var numStations = offsetsGrid.Stations.Count;

        // Initialize coefficient arrays
        var addedMass = new double[numFreq][][];
        var damping = new double[numFreq][][];
        var excitationForce = new double[numFreq][];

        // Water properties
        const double rho = 1025.0; // Seawater density (kg/m³)
        const double g = 9.81;     // Gravitational acceleration (m/s²)

        // For each frequency
        for (int freqIdx = 0; freqIdx < numFreq; freqIdx++)
        {
            var omega = frequencyRange[freqIdx];

            // Initialize 3x3 matrices for this frequency (heave, pitch, roll)
            addedMass[freqIdx] = new double[3][];
            damping[freqIdx] = new double[3][];
            excitationForce[freqIdx] = new double[3];

            for (int i = 0; i < 3; i++)
            {
                addedMass[freqIdx][i] = new double[3];
                damping[freqIdx][i] = new double[3];
            }

            // Arrays for strip integration
            var a33Strip = new double[numStations]; // Heave added mass per station
            var b33Strip = new double[numStations]; // Heave damping per station
            var a55Strip = new double[numStations]; // Pitch added mass moment per station
            var b55Strip = new double[numStations]; // Pitch damping moment per station
            var f3Strip = new double[numStations];  // Heave excitation per station
            var f5Strip = new double[numStations];  // Pitch excitation moment per station

            var stationPositions = new double[numStations];

            // For each station (strip)
            for (int stIdx = 0; stIdx < numStations; stIdx++)
            {
                var stationX = (double)offsetsGrid.Stations[stIdx];
                stationPositions[stIdx] = stationX;

                // Get section shape at draft
                var sectionData = ExtractSectionAtDraft(offsetsGrid, stIdx, draft);

                if (sectionData.Area > 0)
                {
                    // Compute 2D section coefficients using simplified elliptic formulas
                    var coeffs = ComputeSectionCoefficients(sectionData, omega, rho, g);

                    a33Strip[stIdx] = coeffs.A33;
                    b33Strip[stIdx] = coeffs.B33;
                    a55Strip[stIdx] = coeffs.A55;
                    b55Strip[stIdx] = coeffs.B55;
                    f3Strip[stIdx] = coeffs.F3;
                    f5Strip[stIdx] = coeffs.F5;
                }
            }

            // Convert to List<decimal> for integration engine
            var stationPosList = stationPositions.Select(x => (decimal)x).ToList();

            // Integrate along ship length using Simpson's/Trapezoidal rule
            addedMass[freqIdx][0][0] = (double)_integrationEngine.Integrate(stationPosList, a33Strip.Select(v => (decimal)v).ToList());
            damping[freqIdx][0][0] = (double)_integrationEngine.Integrate(stationPosList, b33Strip.Select(v => (decimal)v).ToList());

            addedMass[freqIdx][1][1] = (double)_integrationEngine.Integrate(stationPosList, a55Strip.Select(v => (decimal)v).ToList());
            damping[freqIdx][1][1] = (double)_integrationEngine.Integrate(stationPosList, b55Strip.Select(v => (decimal)v).ToList());

            excitationForce[freqIdx][0] = (double)_integrationEngine.Integrate(stationPosList, f3Strip.Select(v => (decimal)v).ToList());
            excitationForce[freqIdx][1] = (double)_integrationEngine.Integrate(stationPosList, f5Strip.Select(v => (decimal)v).ToList());

            // Roll coefficients (simplified - Phase 5 will add Ikeda damping)
            addedMass[freqIdx][2][2] = addedMass[freqIdx][0][0] * 0.1; // Rough estimate
            damping[freqIdx][2][2] = damping[freqIdx][0][0] * 0.05;
            excitationForce[freqIdx][2] = 0; // Simplified - no roll excitation for head seas
        }

        return new HydrodynamicCoefficients
        {
            Frequency = frequencyRange,
            AddedMass = addedMass,
            Damping = damping,
            ExcitationForce = excitationForce
        };
    }

    /// <summary>
    /// Extract section shape at a given draft.
    /// </summary>
    private SectionData ExtractSectionAtDraft(
        Shared.DTOs.OffsetsGridDto offsetsGrid,
        int stationIndex,
        double draft)
    {
        var waterlineValues = new List<(double z, double y)>();

        // Find waterlines below draft
        for (int wlIdx = 0; wlIdx < offsetsGrid.Waterlines.Count; wlIdx++)
        {
            var z = (double)offsetsGrid.Waterlines[wlIdx];

            if (z <= draft)
            {
                var halfBreadth = (double)offsetsGrid.Offsets[stationIndex][wlIdx];
                waterlineValues.Add((z, halfBreadth));
            }
        }

        if (waterlineValues.Count == 0)
        {
            return new SectionData { Area = 0, Breadth = 0, Height = 0, Centroid = 0 };
        }

        // Compute section properties
        var zValues = waterlineValues.Select(wl => (decimal)wl.z).ToList();
        var yValues = waterlineValues.Select(wl => (decimal)(wl.y * 2)).ToList(); // Full breadth

        // Section area (integrate breadth vs height)
        var area = (double)_integrationEngine.Integrate(zValues, yValues);

        var breadth = waterlineValues.Max(wl => wl.y) * 2; // Maximum breadth
        var height = waterlineValues.Max(wl => wl.z) - waterlineValues.Min(wl => wl.z);

        // Centroid (first moment / area)
        var firstMoment = (double)_integrationEngine.FirstMoment(zValues, yValues);
        var centroid = area > 0 ? firstMoment / area : 0;

        return new SectionData
        {
            Area = area,
            Breadth = breadth,
            Height = height,
            Centroid = centroid
        };
    }

    /// <summary>
    /// Compute 2D hydrodynamic coefficients for a section using simplified elliptic formulas.
    /// Phase 1: Elliptic approximations
    /// Phase 5: Full Lewis conformal transform for accuracy
    /// </summary>
    private SectionCoefficients ComputeSectionCoefficients(
        SectionData section,
        double omega,
        double rho,
        double g)
    {
        // Simplified elliptic formulas for 2D added mass and damping
        // Based on Lewis (1929) approximation for elliptic sections

        var a = section.Breadth / 2;  // Semi-major axis (horizontal)
        var b = section.Height;       // Semi-minor axis (vertical)

        if (a <= 0 || b <= 0)
        {
            return new SectionCoefficients();
        }

        // Wave number
        var k = omega * omega / g;

        // 2D added mass (per unit length)
        // Elliptic section: a33 ≈ ρπab (heave)
        var a33 = rho * Math.PI * a * b;

        // Pitch added mass moment (per unit length): a55 ≈ ρπab * (vertical centroid²)
        var a55 = a33 * Math.Pow(section.Centroid, 2);

        // 2D damping (simplified potential flow damping)
        // b33 ≈ ρg * breadth / omega (simplified)
        var b33 = rho * g * section.Breadth / omega;
        var b55 = b33 * Math.Pow(section.Centroid, 2);

        // Wave excitation forces (Froude-Krylov + diffraction)
        // F3 ≈ ρgA * e^(kz) (simplified)
        var depthFactor = Math.Exp(k * section.Centroid);
        var f3 = rho * g * section.Area * depthFactor;
        var f5 = f3 * section.Centroid; // Moment arm

        return new SectionCoefficients
        {
            A33 = a33,
            B33 = b33,
            A55 = a55,
            B55 = b55,
            F3 = f3,
            F5 = f5
        };
    }

    private class SectionData
    {
        public double Area { get; set; }
        public double Breadth { get; set; }
        public double Height { get; set; }
        public double Centroid { get; set; }
    }

    private class SectionCoefficients
    {
        public double A33 { get; set; } // Heave added mass
        public double B33 { get; set; } // Heave damping
        public double A55 { get; set; } // Pitch added mass moment
        public double B55 { get; set; } // Pitch damping moment
        public double F3 { get; set; }  // Heave excitation
        public double F5 { get; set; }  // Pitch excitation moment
    }
}
