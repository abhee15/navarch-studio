using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataService.Data;
using DataService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace DataService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/benchmarks")]
public class BenchmarksController : ControllerBase
{
    private readonly DataDbContext _db;
    private readonly IBenchmarkIngestionService _ingest;
    private readonly BenchmarkSeedService _seed;
    private readonly BenchmarkValidationService _validator;

    public BenchmarksController(DataDbContext db, IBenchmarkIngestionService ingest, BenchmarkSeedService seed, BenchmarkValidationService validator)
    {
        _db = db;
        _ingest = ingest;
        _seed = seed;
        _validator = validator;
    }

    [HttpGet("cases")]
    public async Task<IActionResult> GetCases(CancellationToken ct)
    {
        var items = await _db.BenchmarkCases
            .AsNoTracking()
            .Select(c => new { c.Slug, c.Title, c.Description })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("cases/{slug}")]
    public async Task<IActionResult> GetCase(string slug, CancellationToken ct)
    {
        var entity = await _db.BenchmarkCases.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, ct);
        if (entity == null) return NotFound();

        var geoms = await _db.BenchmarkGeometries.AsNoTracking().Where(g => g.CaseId == entity.Id).ToListAsync(ct);
        var tps = await _db.BenchmarkTestPoints.AsNoTracking().Where(t => t.CaseId == entity.Id).OrderBy(t => t.Fr).ToListAsync(ct);
        var refs = await _db.BenchmarkMetricRefs.AsNoTracking().Where(m => m.CaseId == entity.Id).ToListAsync(ct);
        var assets = await _db.BenchmarkAssets.AsNoTracking().Where(a => a.CaseId == entity.Id).ToListAsync(ct);

        return Ok(new { entity.Slug, entity.Title, entity.Description, Geometries = geoms, TestPoints = tps, MetricRefs = refs, Assets = assets });
    }

    public record ValidateRequest(decimal? Fr, decimal? Ct = null, decimal? Sinkage = null, decimal? Trim = null, decimal? Cw = null);

    [HttpPost("cases/{slug}/validate")]
    public async Task<IActionResult> Validate(string slug, [FromBody] ValidateRequest req, CancellationToken ct)
    {
        var results = await _validator.ValidateAsync(slug, new BenchmarkValidationService.ValidationInput(req.Fr, req.Ct, req.Sinkage, req.Trim, req.Cw), ct);
        return Ok(results);
    }

    [HttpPost("cases/{slug}/ingest")]
    public async Task<IActionResult> Ingest(string slug, CancellationToken ct)
    {
        // Simple guard to avoid accidental triggers; extend with auth in production
        if (slug == "kcs") await _ingest.IngestKcsAsync(ct);
        else if (slug == "kvlcc2") await _ingest.IngestKvlcc2Async(ct);
        else if (slug == "wigley") await _ingest.IngestWigleyAsync(ct);
        else return NotFound();

        await _seed.SeedAsync(ct);
        return Accepted(new { message = "Ingestion started" });
    }

    [HttpGet("cases/{slug}/validations")]
    public async Task<IActionResult> GetValidations(string slug, CancellationToken ct)
    {
        var entity = await _db.BenchmarkCases.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, ct);
        if (entity == null) return NotFound();
        var runs = await _db.BenchmarkValidationRuns.AsNoTracking().Where(v => v.CaseId == entity.Id)
            .OrderByDescending(v => v.CreatedAt).Take(50).ToListAsync(ct);
        return Ok(runs);
    }
}
