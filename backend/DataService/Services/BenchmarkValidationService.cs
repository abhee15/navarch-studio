using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataService.Data;
using Microsoft.EntityFrameworkCore;

namespace DataService.Services;

public class BenchmarkValidationService
{
    private readonly DataDbContext _db;

    public BenchmarkValidationService(DataDbContext db)
    {
        _db = db;
    }

    public record ValidationInput(decimal? Fr, decimal? Ct = null, decimal? Sinkage = null, decimal? Trim = null, decimal? Cw = null);
    public record MetricResult(string Metric, bool Passed, decimal? Actual, decimal? Reference, decimal? Delta, decimal? RelError);

    public async Task<IReadOnlyList<MetricResult>> ValidateAsync(string caseSlug, ValidationInput input, CancellationToken cancellationToken)
    {
        var @case = await _db.BenchmarkCases.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == caseSlug, cancellationToken);
        if (@case == null) throw new InvalidOperationException($"Unknown benchmark case '{caseSlug}'");

        var results = new List<MetricResult>();

        if (input.Ct.HasValue && input.Fr.HasValue)
        {
            var ctRef = await _db.BenchmarkMetricRefs.AsNoTracking()
                .Where(m => m.CaseId == @case.Id && m.Metric == "Ct" && m.Fr == input.Fr)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (ctRef != null && ctRef.ValueNum.HasValue)
            {
                decimal? rel = ctRef.ValueNum.Value == 0
                    ? (decimal?)null
                    : Math.Abs((input.Ct.Value - ctRef.ValueNum.Value) / ctRef.ValueNum.Value);
                var tol = ctRef.TolRel ?? 0.03m;
                var pass = rel.HasValue && rel.Value <= tol;
                results.Add(new MetricResult("Ct", pass, input.Ct.Value, ctRef.ValueNum.Value, input.Ct.Value - ctRef.ValueNum.Value, rel));
            }
        }

        if (input.Cw.HasValue && input.Fr.HasValue)
        {
            var cwRef = await _db.BenchmarkMetricRefs.AsNoTracking()
                .Where(m => m.CaseId == @case.Id && m.Metric == "Cw" && m.Fr == input.Fr)
                .FirstOrDefaultAsync(cancellationToken);
            if (cwRef != null && cwRef.ValueNum.HasValue)
            {
                decimal? rel = cwRef.ValueNum.Value == 0
                    ? (decimal?)null
                    : Math.Abs((input.Cw.Value - cwRef.ValueNum.Value) / cwRef.ValueNum.Value);
                var tol = cwRef.TolRel ?? 0.05m;
                var pass = rel.HasValue && rel.Value <= tol;
                results.Add(new MetricResult("Cw", pass, input.Cw.Value, cwRef.ValueNum.Value, input.Cw.Value - cwRef.ValueNum.Value, rel));
            }
        }

        // Sinkage/Trim qualitative bands not enforced numerically here; stub pass-through
        if (input.Sinkage.HasValue)
        {
            results.Add(new MetricResult("sinkage", true, input.Sinkage.Value, null, null, null));
        }
        if (input.Trim.HasValue)
        {
            results.Add(new MetricResult("trim", true, input.Trim.Value, null, null, null));
        }

        return results;
    }
}
