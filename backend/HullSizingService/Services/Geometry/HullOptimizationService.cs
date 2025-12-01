using System;
using System.Collections.Generic;
using System.Linq;
using HullSizingService.Services.Geometry;
using HullSizingService.Services.Integration;
using Microsoft.Extensions.Logging;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Inverse Design Optimization Service
/// Adjusts Control Point positions to meet target hydrostatic properties (CB, CP, LCB)
/// Uses Differential Evolution algorithm for gradient-free global optimization
/// </summary>
public class HullOptimizationService
{
    private readonly ILogger<HullOptimizationService> _logger;
    private readonly IHydrostaticsCalculator _hydrostaticsCalculator;

    public HullOptimizationService(
        ILogger<HullOptimizationService> logger,
        IHydrostaticsCalculator hydrostaticsCalculator)
    {
        _logger = logger;
        _hydrostaticsCalculator = hydrostaticsCalculator;
    }

    /// <summary>
    /// Optimization targets
    /// </summary>
    public class OptimizationTargets
    {
        public decimal CbTarget { get; set; }
        public decimal CpTarget { get; set; }
        public decimal LcbTargetPercent { get; set; } // LCB as % of Lpp from midship (positive = forward)
        public decimal LppM { get; set; }
        public decimal BeamM { get; set; }
        public decimal DraftM { get; set; }
    }

    /// <summary>
    /// Optimization result
    /// </summary>
    public class OptimizationResult
    {
        public NurbsSurfaceGenerator.ControlPointGrid OptimalControlPoints { get; set; } = null!;
        public decimal FinalError { get; set; }
        public decimal FinalCb { get; set; }
        public decimal FinalCp { get; set; }
        public decimal FinalLcbPercent { get; set; }
        public int Iterations { get; set; }
        public bool Converged { get; set; }
    }

    /// <summary>
    /// Optimizes Control Point Grid to meet hydrostatic targets
    /// </summary>
    public OptimizationResult Optimize(
        NurbsSurfaceGenerator.ControlPointGrid initialControlPoints,
        OptimizationTargets targets,
        OptimizationOptions? options = null)
    {
        options ??= new OptimizationOptions();

        _logger.LogInformation(
            "[HULL_OPTIMIZATION] Starting optimization: CB={CbTarget}, CP={CpTarget}, LCB={LcbTarget}%",
            targets.CbTarget, targets.CpTarget, targets.LcbTargetPercent);

        // Initialize Differential Evolution population
        var population = InitializePopulation(initialControlPoints, options.PopulationSize);

        // Track best solution
        var bestSolution = initialControlPoints;
        var bestError = EvaluateObjectiveFunction(initialControlPoints, targets);

        // Optimization loop
        for (int iteration = 0; iteration < options.MaxIterations; iteration++)
        {
            // Create new generation using Differential Evolution
            var newPopulation = new List<NurbsSurfaceGenerator.ControlPointGrid>();

            foreach (var individual in population)
            {
                // Mutation: Create mutant vector
                var mutant = CreateMutant(population, individual, options.MutationFactor, options.CrossoverRate);

                // Evaluate mutant
                var mutantError = EvaluateObjectiveFunction(mutant, targets);

                // Selection: Keep better solution
                var currentError = EvaluateObjectiveFunction(individual, targets);
                if (mutantError < currentError)
                {
                    newPopulation.Add(mutant);
                    if (mutantError < bestError)
                    {
                        bestSolution = CloneControlPointGrid(mutant);
                        bestError = mutantError;
                    }
                }
                else
                {
                    newPopulation.Add(individual);
                }
            }

            population = newPopulation;

            // Log progress
            if (iteration % options.LogInterval == 0)
            {
                var currentCb = ComputeHydrostatics(bestSolution, targets).Cb;
                var currentCp = ComputeHydrostatics(bestSolution, targets).Cp;
                var currentLcb = ComputeHydrostatics(bestSolution, targets).LcbPercent;

                _logger.LogDebug(
                    "[HULL_OPTIMIZATION] Iteration {Iteration}/{MaxIterations}: Error={Error}, CB={Cb}, CP={Cp}, LCB={Lcb}%",
                    iteration, options.MaxIterations, bestError, currentCb, currentCp, currentLcb);
            }

            // Check convergence
            if (bestError < options.Tolerance)
            {
                _logger.LogInformation(
                    "[HULL_OPTIMIZATION] Converged at iteration {Iteration} with error {Error}",
                    iteration, bestError);
                break;
            }
        }

        // Compute final hydrostatics
        var finalHydrostatics = ComputeHydrostatics(bestSolution, targets);

        return new OptimizationResult
        {
            OptimalControlPoints = bestSolution,
            FinalError = bestError,
            FinalCb = finalHydrostatics.Cb,
            FinalCp = finalHydrostatics.Cp,
            FinalLcbPercent = finalHydrostatics.LcbPercent,
            Iterations = options.MaxIterations,
            Converged = bestError < options.Tolerance
        };
    }

    /// <summary>
    /// Objective function: f(P) = wB(CB_calc - CB_target)² + wP(CP_calc - CP_target)² + wL(LCB_calc - LCB_target)²
    /// </summary>
    private decimal EvaluateObjectiveFunction(
        NurbsSurfaceGenerator.ControlPointGrid controlPoints,
        OptimizationTargets targets)
    {
        var hydrostatics = ComputeHydrostatics(controlPoints, targets);

        // Weighting factors (LCB is more sensitive, so higher weight)
        decimal wB = 1.0m; // Block coefficient weight
        decimal wP = 1.0m; // Prismatic coefficient weight
        decimal wL = 2.0m; // LCB weight (higher because it's more sensitive)

        decimal errorCb = hydrostatics.Cb - targets.CbTarget;
        decimal errorCp = hydrostatics.Cp - targets.CpTarget;
        decimal errorLcb = hydrostatics.LcbPercent - targets.LcbTargetPercent;

        decimal objective = wB * errorCb * errorCb +
                          wP * errorCp * errorCp +
                          wL * errorLcb * errorLcb;

        return objective;
    }

    /// <summary>
    /// Computes hydrostatic properties from Control Point Grid
    /// Uses Gauss Quadrature for fast, accurate evaluation in optimization loop
    /// </summary>
    private (decimal Cb, decimal Cp, decimal LcbPercent) ComputeHydrostatics(
        NurbsSurfaceGenerator.ControlPointGrid controlPoints,
        OptimizationTargets targets)
    {
        // Use direct NURBS evaluation with Gauss Quadrature (fast path)
        if (_hydrostaticsCalculator is NurbsHydrostaticsCalculator nurbsCalc)
        {
            return nurbsCalc.ComputeFromControlPointGrid(
                controlPoints,
                targets.LppM,
                targets.BeamM,
                targets.DraftM,
                useGaussQuadrature: true);
        }

        // Fallback: generate offsets and use discrete integration
        var stations = GenerateStationPositions(controlPoints.NumStations);
        var waterlines = GenerateWaterlinePositions(controlPoints.NumControlPointsPerStation);

        var offsets = NurbsSurfaceGenerator.GenerateOffsetsFromSurface(
            controlPoints,
            stations,
            waterlines,
            targets.LppM,
            targets.BeamM,
            targets.DraftM);

        return _hydrostaticsCalculator.ComputeFromOffsets(
            stations,
            waterlines,
            offsets,
            targets.LppM,
            targets.BeamM,
            targets.DraftM);
    }

    /// <summary>
    /// Initializes Differential Evolution population
    /// </summary>
    private List<NurbsSurfaceGenerator.ControlPointGrid> InitializePopulation(
        NurbsSurfaceGenerator.ControlPointGrid initial,
        int populationSize)
    {
        var population = new List<NurbsSurfaceGenerator.ControlPointGrid> { initial };

        // Add random variations of initial guess
        var random = new Random();
        for (int i = 1; i < populationSize; i++)
        {
            var variant = CloneControlPointGrid(initial);

            // Add small random perturbations to control points
            for (int station = 0; station < variant.NumStations; station++)
            {
                for (int cp = 0; cp < variant.NumControlPointsPerStation; cp++)
                {
                    var original = variant.Points[station][cp];
                    // Perturb y (half-breadth) by ±5%
                    decimal perturbation = (decimal)(random.NextDouble() * 0.1 - 0.05);
                    variant.Points[station][cp] = (
                        original.x,
                        original.y * (1m + perturbation),
                        original.z
                    );
                }
            }

            population.Add(variant);
        }

        return population;
    }

    /// <summary>
    /// Creates mutant vector using Differential Evolution mutation strategy
    /// </summary>
    private NurbsSurfaceGenerator.ControlPointGrid CreateMutant(
        List<NurbsSurfaceGenerator.ControlPointGrid> population,
        NurbsSurfaceGenerator.ControlPointGrid target,
        decimal mutationFactor,
        decimal crossoverRate)
    {
        var random = new Random();
        var mutant = CloneControlPointGrid(target);

        // Select three random distinct individuals (different from target)
        var candidates = population.Where(p => p != target).ToList();
        if (candidates.Count < 3)
        {
            return mutant; // Not enough diversity
        }

        var shuffled = candidates.OrderBy(_ => random.Next()).Take(3).ToList();
        var r1 = shuffled[0];
        var r2 = shuffled[1];
        var r3 = shuffled[2];

        // Mutation: mutant = r1 + F * (r2 - r3)
        // Apply to y coordinates (half-breadth) only
        for (int station = 0; station < mutant.NumStations; station++)
        {
            for (int cp = 0; cp < mutant.NumControlPointsPerStation; cp++)
            {
                if (random.NextDouble() < (double)crossoverRate)
                {
                    var r1Point = r1.Points[station][cp];
                    var r2Point = r2.Points[station][cp];
                    var r3Point = r3.Points[station][cp];

                    // Mutate y coordinate (half-breadth)
                    decimal mutatedY = r1Point.y + mutationFactor * (r2Point.y - r3Point.y);

                    // Ensure non-negative
                    mutatedY = Math.Max(0m, mutatedY);

                    mutant.Points[station][cp] = (r1Point.x, mutatedY, r1Point.z);
                }
            }
        }

        return mutant;
    }

    /// <summary>
    /// Clones a Control Point Grid
    /// </summary>
    private NurbsSurfaceGenerator.ControlPointGrid CloneControlPointGrid(
        NurbsSurfaceGenerator.ControlPointGrid source)
    {
        var clone = new NurbsSurfaceGenerator.ControlPointGrid(source.NumStations, source.NumControlPointsPerStation);

        for (int i = 0; i < source.NumStations; i++)
        {
            for (int j = 0; j < source.NumControlPointsPerStation; j++)
            {
                clone.Points[i].Add(source.Points[i][j]);
            }
        }

        return clone;
    }

    /// <summary>
    /// Generates uniform station positions [0, 1]
    /// </summary>
    private List<decimal> GenerateStationPositions(int numStations)
    {
        var positions = new List<decimal>();
        for (int i = 0; i < numStations; i++)
        {
            positions.Add((decimal)i / (numStations - 1));
        }
        return positions;
    }

    /// <summary>
    /// Generates uniform waterline positions [0, 1]
    /// </summary>
    private List<decimal> GenerateWaterlinePositions(int numWaterlines)
    {
        var positions = new List<decimal>();
        for (int i = 0; i < numWaterlines; i++)
        {
            positions.Add((decimal)i / (numWaterlines - 1));
        }
        return positions;
    }

    /// <summary>
    /// Optimization algorithm parameters
    /// </summary>
    public class OptimizationOptions
    {
        public int PopulationSize { get; set; } = 20;
        public int MaxIterations { get; set; } = 100;
        public decimal MutationFactor { get; set; } = 0.5m; // F parameter in DE
        public decimal CrossoverRate { get; set; } = 0.7m; // CR parameter in DE
        public decimal Tolerance { get; set; } = 0.001m; // Convergence tolerance
        public int LogInterval { get; set; } = 10; // Log every N iterations
    }
}

/// <summary>
/// Interface for computing hydrostatics from NURBS geometry
/// Supports both direct Control Point Grid evaluation (fast) and discrete offsets (compatibility)
/// </summary>
public interface IHydrostaticsCalculator
{
    /// <summary>
    /// Computes hydrostatics from discrete offsets (compatibility method)
    /// </summary>
    (decimal Cb, decimal Cp, decimal LcbPercent) ComputeFromOffsets(
        List<decimal> stations,
        List<decimal> waterlines,
        Dictionary<(int stationIdx, int waterlineIdx), decimal> offsets,
        decimal lppM,
        decimal beamM,
        decimal draftM);
}
