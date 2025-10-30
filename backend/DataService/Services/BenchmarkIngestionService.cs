using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services;

public class BenchmarkIngestionService : IBenchmarkIngestionService
{
    private readonly IAmazonS3 _s3;
    private readonly DataDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BenchmarkIngestionService> _logger;
    private readonly string _rawBucket;
    private readonly string _curatedBucket;

    public BenchmarkIngestionService(
        IAmazonS3 s3,
        DataDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<BenchmarkIngestionService> logger)
    {
        _s3 = s3;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _rawBucket = config["Benchmark:RawBucket"] ?? string.Empty;
        _curatedBucket = config["Benchmark:CuratedBucket"] ?? string.Empty;
    }

    public async Task IngestKcsAsync(CancellationToken cancellationToken)
    {
        await EnsureBucketsConfiguredAsync(cancellationToken);
        var caseEntity = await UpsertCaseAsync("kcs", "KCS (KRISO Container Ship)", cancellationToken);
        // Register source geometry reference
        await UpsertGeometryAsync(caseEntity.Id, "offsets", "https://www.nmri.go.jp", cancellationToken);
        // Canonical test points (Tokyo 2015 Case 2.1)
        var frs = new[] { 0.108m, 0.152m, 0.195m, 0.227m, 0.260m, 0.282m };
        foreach (var fr in frs)
        {
            await UpsertTestPointAsync(caseEntity.Id, fr, null, cancellationToken);
        }
        _logger.LogInformation("KCS ingestion initialized for case {CaseId}", caseEntity.Id);
    }

    public async Task IngestKvlcc2Async(CancellationToken cancellationToken)
    {
        await EnsureBucketsConfiguredAsync(cancellationToken);
        var caseEntity = await UpsertCaseAsync("kvlcc2", "KVLCC2 (Tanker)", cancellationToken);
        await UpsertGeometryAsync(caseEntity.Id, "iges", "https://www.simman2020.kr", cancellationToken);
        _logger.LogInformation("KVLCC2 ingestion initialized for case {CaseId}", caseEntity.Id);
    }

    public async Task IngestWigleyAsync(CancellationToken cancellationToken)
    {
        await EnsureBucketsConfiguredAsync(cancellationToken);
        var caseEntity = await UpsertCaseAsync("wigley", "Wigley (Parabolic Hull)", cancellationToken);
        await UpsertGeometryAsync(caseEntity.Id, "analytic", "https://eprints.soton.ac.uk", cancellationToken);
        // Common reference speeds for validation examples
        var frs = new[] { 0.250m, 0.300m, 0.408m };
        foreach (var fr in frs)
        {
            await UpsertTestPointAsync(caseEntity.Id, fr, null, cancellationToken);
        }
        _logger.LogInformation("Wigley ingestion initialized for case {CaseId}", caseEntity.Id);
    }

    private async Task EnsureBucketsConfiguredAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_rawBucket) || string.IsNullOrWhiteSpace(_curatedBucket))
        {
            throw new InvalidOperationException("Benchmark buckets are not configured (Benchmark:RawBucket, Benchmark:CuratedBucket)");
        }

        // no-op placeholder to validate access
        await _s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = _rawBucket, MaxKeys = 1 }, cancellationToken);
    }

    private async Task<BenchmarkCase> UpsertCaseAsync(string slug, string title, CancellationToken cancellationToken)
    {
        var existing = await _db.BenchmarkCases.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var entity = new BenchmarkCase
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            CreatedAt = DateTime.UtcNow
        };
        _db.BenchmarkCases.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task UpsertGeometryAsync(Guid caseId, string type, string sourceUrl, CancellationToken cancellationToken)
    {
        var exists = await _db.BenchmarkGeometries.FirstOrDefaultAsync(g => g.CaseId == caseId && g.Type == type, cancellationToken);
        if (exists != null)
        {
            return;
        }
        var geom = new BenchmarkGeometry
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Type = type,
            SourceUrl = sourceUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.BenchmarkGeometries.Add(geom);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertTestPointAsync(Guid caseId, decimal fr, decimal? vm, CancellationToken cancellationToken)
    {
        var exists = await _db.BenchmarkTestPoints.FirstOrDefaultAsync(t => t.CaseId == caseId && t.Fr == fr, cancellationToken);
        if (exists != null)
        {
            return;
        }
        var tp = new BenchmarkTestPoint
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Fr = fr,
            Vm = vm,
            CreatedAt = DateTime.UtcNow
        };
        _db.BenchmarkTestPoints.Add(tp);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
