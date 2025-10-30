using System;
using System.Threading;
using System.Threading.Tasks;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services;

public class BenchmarkSeedService
{
    private readonly DataDbContext _db;
    private readonly ILogger<BenchmarkSeedService> _logger;

    public BenchmarkSeedService(DataDbContext db, ILogger<BenchmarkSeedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedKcsAsync(cancellationToken);
        await SeedWigleyAsync(cancellationToken);
        await SeedKvlcc2Async(cancellationToken);
    }

    private async Task SeedKcsAsync(CancellationToken cancellationToken)
    {
        var kcs = await _db.BenchmarkCases.FirstOrDefaultAsync(c => c.Slug == "kcs", cancellationToken);
        if (kcs == null) return;

        await UpsertMetricAsync(kcs.Id, 0.260m, "Ct", 0.003711m, null, 0.03m,
            "Table 14 (Tokyo 2015)", "https://t2015.nmri.go.jp", cancellationToken);

        // Qualitative: sinkage/trim bands via figures
        await UpsertMetricAsync(kcs.Id, 0.260m, "sinkage", null, null, null,
            "Figures 11–12 (Tokyo 2015)", "https://t2015.nmri.go.jp", cancellationToken);
        await UpsertMetricAsync(kcs.Id, 0.260m, "trim", null, null, null,
            "Figures 11–12 (Tokyo 2015)", "https://t2015.nmri.go.jp", cancellationToken);
    }

    private async Task SeedWigleyAsync(CancellationToken cancellationToken)
    {
        var wigley = await _db.BenchmarkCases.FirstOrDefaultAsync(c => c.Slug == "wigley", cancellationToken);
        if (wigley == null) return;

        await UpsertMetricAsync(wigley.Id, 0.250m, "Ct", null, null, null,
            "Fig. 5 (Southampton)", "https://eprints.soton.ac.uk", cancellationToken);
        await UpsertMetricAsync(wigley.Id, 0.408m, "Ct", null, null, null,
            "Fig. 6 (Southampton)", "https://eprints.soton.ac.uk", cancellationToken);

        await UpsertMetricAsync(wigley.Id, 0.300m, "Cw", 0.00132m, null, null,
            "SeaFEM note", "https://compassis.com", cancellationToken);
    }

    private async Task SeedKvlcc2Async(CancellationToken cancellationToken)
    {
        var kv = await _db.BenchmarkCases.FirstOrDefaultAsync(c => c.Slug == "kvlcc2", cancellationToken);
        if (kv == null) return;

        // Qualitative wave pattern acceptance reference
        await UpsertMetricAsync(kv.Id, 0.142m, "wave_pattern", null, null, null,
            "Gothenburg 2010 Fig. 3", "https://upcommons.upc.edu", cancellationToken);

        // POW curves placeholder indicator (numeric curves to be attached later)
        await UpsertMetricAsync(kv.Id, null, "pow_curve", null, null, null,
            "HMRI KVLCC2 POW", "https://www.simman2014.dk", cancellationToken);
    }

    private async Task UpsertMetricAsync(Guid caseId, decimal? fr, string metric, decimal? valueNum, string? unit, decimal? tolRel, string? figureRef, string? sourceUrl, CancellationToken cancellationToken)
    {
        var exists = await _db.BenchmarkMetricRefs.FirstOrDefaultAsync(m => m.CaseId == caseId && m.Metric == metric && m.Fr == fr, cancellationToken);
        if (exists != null) return;

        var mref = new BenchmarkMetricRef
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Fr = fr,
            Metric = metric,
            ValueNum = valueNum,
            Unit = unit,
            TolRel = tolRel,
            FigureRef = figureRef,
            SourceUrl = sourceUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.BenchmarkMetricRefs.Add(mref);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
