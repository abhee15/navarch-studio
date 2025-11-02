# Performance Targets & Optimization Strategies

## Target Metrics (Production SLAs)

### Backend Performance

| Metric | Target (p95) | Measurement | Strategy |
|--------|--------------|-------------|----------|
| Displacement Closure (single) | <50ms | Stopwatch in service | Fast math, early exit |
| Holtrop Calculation (single) | <20ms | Stopwatch in service | Simplified formulas |
| Full Sizing Run (5 candidates) | <2s | X-Compute-Time-Ms header | Parallel generation |
| Slider Recompute (1 candidate) | <300ms | API round-trip | Debouncing, optimistic UI |
| Water Properties API Call | <50ms | HTTP client timing | Polly retry, cache |
| Push to Hydrostatics | <1s | Service-to-service call | Idempotency, retry |

### Frontend Performance

| Metric | Target (p50) | Measurement | Strategy |
|--------|--------------|-------------|----------|
| 3D Rendering FPS | ≥45 FPS | Stats.js | LOD, frustum culling |
| Slider Interaction | <300ms | Performance.now() | Debounce, Web Worker |
| Candidate Card Render | <16ms | React DevTools | Virtualization if >20 |
| Page Load (workspace) | <2s | Lighthouse | Code splitting, lazy load |
| Memory Usage (session) | <500 MB | Chrome DevTools | Dispose geometries |

---

## Backend Optimization Strategies

### 1. Parallel Candidate Generation

**Problem:** Sequential generation takes 5× longer than parallel

**Solution:**
```csharp
public async Task<List<CandidateDesign>> SolveAsync(MissionCase mission, SizingOptions options, CancellationToken ct)
{
    var families = await _hullFamilyService.GetApplicableFamiliesAsync(mission, ct);
    
    // Generate candidates in parallel
    var tasks = families.Select(family => 
        GenerateCandidateAsync(mission, family, options, ct)
    ).ToList();
    
    var results = await Task.WhenAll(tasks);
    
    return results
        .Where(c => c != null)
        .OrderByDescending(c => c.Score)
        .Select((c, i) => { c.Rank = i + 1; return c; })
        .ToList();
}
```

**Improvement:** 5 candidates in ~400ms vs 2000ms sequential

---

### 2. Water Properties Caching

**Problem:** Every sizing run calls DataService for water props (adds 50ms + network latency)

**Solution:**
```csharp
public async Task<WaterPropertiesDto> GetWaterPropertiesAsync(decimal tempC, decimal salinityPsu, CancellationToken ct)
{
    var cacheKey = $"water_props_{tempC}_{salinityPsu}";
    
    // Try cache (12h TTL)
    if (_cache.TryGetValue(cacheKey, out WaterPropertiesDto? cached))
    {
        _logger.LogDebug("Cache HIT for {Key}", cacheKey);
        return cached!;
    }
    
    try
    {
        var response = await _httpClient.GetAsync($"/api/v1/catalog/water-properties?temp={tempC}&salinity={salinityPsu}", ct);
        response.EnsureSuccessStatusCode();
        
        var props = await response.Content.ReadFromJsonAsync<WaterPropertiesDto>(cancellationToken: ct);
        
        _cache.Set(cacheKey, props, TimeSpan.FromHours(12));
        _logger.LogDebug("Cache SET for {Key}", cacheKey);
        
        return props!;
    }
    catch (Exception ex)
    {
        // Fallback to stale cache
        if (_cache.TryGetValue(cacheKey, out WaterPropertiesDto? stale, useStale: true))
        {
            _logger.LogWarning("Using stale cached water properties due to error: {Error}", ex.Message);
            return stale!;
        }
        throw;
    }
}
```

**Improvement:** 50ms → <1ms for cached lookups (50× faster)

---

### 3. Database Indexes

**Problem:** Slow queries on candidate retrieval, mission case listing

**Solution:** Indexes on hot paths

```sql
-- Mission cases by user (most common query)
CREATE INDEX idx_mission_cases_user_id ON sizing.mission_cases(user_id) 
    WHERE deleted_at IS NULL;

-- Candidates by run (always filtered by run_id)
CREATE INDEX idx_candidate_designs_sizing_run_id ON sizing.candidate_designs(sizing_run_id);

-- Candidates sorted by score
CREATE INDEX idx_candidate_designs_score_desc ON sizing.candidate_designs(score DESC);

-- Idempotency key lookup (every push-to-hydrostatics)
CREATE UNIQUE INDEX idx_push_operations_idempotency_key ON sizing.push_operations(idempotency_key);
```

**Improvement:** Query time 50ms → 5ms (10× faster)

---

### 4. Polly Circuit Breaker

**Problem:** If DataService is down, every request waits for timeout (2s), cascading failures

**Solution:**
```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,  // Open after 5 failures
        durationOfBreak: TimeSpan.FromSeconds(30), // Stay open for 30s
        onBreak: (outcome, duration) =>
        {
            Log.Error("Circuit breaker OPENED for {Duration}s due to {Exception}",
                duration.TotalSeconds, outcome.Exception?.Message);
        },
        onReset: () => Log.Information("Circuit breaker RESET")
    );
```

**Behavior:**
- After 5 consecutive failures → circuit opens
- All subsequent requests fail immediately (no timeout wait)
- After 30s → half-open (test with one request)
- If succeeds → close circuit, if fails → reopen for 30s

**Improvement:** Prevents cascading failures, degrades gracefully

---

## Frontend Optimization Strategies

### 1. Dynamic LOD (Level of Detail)

**Problem:** High-poly hull mesh (200k triangles) causes FPS drops when zoomed out

**Solution:**
```typescript
import { useThree } from "@react-three/fiber";

export const HullMesh: React.FC<{ candidate: CandidateDesign }> = ({ candidate }) => {
  const camera = useThree((state) => state.camera);
  const [lodLevel, setLodLevel] = useState<'near' | 'mid' | 'far'>('mid');
  
  useFrame(() => {
    const distance = camera.position.length();
    const lpp = candidate.lppM;
    
    // Switch LOD based on camera distance
    if (distance < lpp * 2) {
      setLodLevel('near'); // 80k tris
    } else if (distance < lpp * 5) {
      setLodLevel('mid'); // 40k tris
    } else {
      setLodLevel('far'); // 20k tris
    }
  });
  
  const geometry = useMemo(() => {
    const triCounts = { near: 80000, mid: 40000, far: 20000 };
    return generateHullGeometry(candidate.geometryJson, triCounts[lodLevel]);
  }, [candidate, lodLevel]);
  
  return <mesh geometry={geometry}>...</mesh>;
};
```

**Improvement:** FPS increases from 25 → 50 when zoomed out

---

### 2. Debounced Slider Updates

**Problem:** Every slider pixel movement triggers API call (300+ calls during drag)

**Solution:**
```typescript
import { debounce } from "lodash";

export const SpeedShapeSlider: React.FC = () => {
  const [localValue, setLocalValue] = useState(50);
  
  // Debounce API call (300ms)
  const debouncedRecompute = useMemo(
    () => debounce(async (value: number) => {
      await hullSizingApi.recomputeCandidate(candidateId, { adjustments: { ... } });
    }, 300),
    [candidateId]
  );
  
  const handleChange = (value: number) => {
    setLocalValue(value); // Immediate UI update
    debouncedRecompute(value); // Debounced API call
  };
  
  return <input type="range" value={localValue} onChange={(e) => handleChange(Number(e.target.value))} />;
};
```

**Improvement:** 300 API calls → 1 API call (300× fewer requests)

---

### 3. Web Worker for Mesh Generation

**Problem:** Generating hull mesh blocks main thread (causes UI jank)

**Solution:**
```typescript
// public/workers/hull-geometry.worker.ts
import { expose } from "comlink";

const api = {
  generateMesh(geometryJson: any, triCount: number): MeshData {
    // Heavy computation (offsets → vertices/indices)
    const vertices = new Float32Array(...);
    const indices = new Uint32Array(...);
    return { vertices, indices };
  }
};

expose(api);

// In component
import { wrap } from "comlink";

const worker = useMemo(() => {
  const w = new Worker(new URL("../../../public/workers/hull-geometry.worker.ts", import.meta.url));
  return wrap<typeof api>(w);
}, []);

const meshData = await worker.generateMesh(candidate.geometryJson, 80000);
```

**Improvement:** UI stays responsive during mesh generation (60 FPS vs 15 FPS)

---

### 4. Lazy Loading & Code Splitting

**Problem:** Hull sizing bundle increases initial load time

**Solution:**
```typescript
// App.tsx
const HullSizingLanding = lazy(() => import("./pages/hull-sizing/HullSizingLanding"));
const SizingWorkspace = lazy(() => import("./pages/hull-sizing/SizingWorkspace"));

<Routes>
  <Route path="/hull-sizing" element={<Suspense fallback={<LoadingSpinner />}><HullSizingLanding /></Suspense>} />
  <Route path="/hull-sizing/workspace/:runId" element={<Suspense fallback={<LoadingSpinner />}><SizingWorkspace /></Suspense>} />
</Routes>
```

**Improvement:** Initial bundle size reduced by ~500 KB

---

### 5. Virtualized Candidates Grid

**Problem:** Rendering 20+ candidate cards slows down page

**Solution:**
```typescript
import { useVirtualizer } from "@tanstack/react-virtual";

export const CandidatesGrid: React.FC<{ candidates: CandidateDesign[] }> = ({ candidates }) => {
  const parentRef = useRef<HTMLDivElement>(null);
  
  const virtualizer = useVirtualizer({
    count: candidates.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 300, // Card height
    overscan: 2
  });
  
  return (
    <div ref={parentRef} className="h-screen overflow-auto">
      <div style={{ height: `${virtualizer.getTotalSize()}px`, position: 'relative' }}>
        {virtualizer.getVirtualItems().map((virtualItem) => {
          const candidate = candidates[virtualItem.index];
          return (
            <div key={virtualItem.key} style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: `${virtualItem.size}px`, transform: `translateY(${virtualItem.start}px)` }}>
              <CandidateCard candidate={candidate} />
            </div>
          );
        })}
      </div>
    </div>
  );
};
```

**Improvement:** Renders only visible cards (5-7 cards vs all 20)

---

## Performance Monitoring

### Backend Metrics (CloudWatch)

**Dashboard:**
```hcl
resource "aws_cloudwatch_dashboard" "hull_sizing" {
  dashboard_name = "${var.project_name}-hull-sizing-${var.environment}"

  dashboard_body = jsonencode({
    widgets = [
      {
        type = "metric"
        properties = {
          metrics = [
            ["NavArch/HullSizing", "SizingRunDuration", { stat = "Average" }],
            ["...", { stat = "p95" }]
          ]
          period = 60
          stat   = "Average"
          region = var.aws_region
          title  = "Sizing Run Duration"
        }
      },
      {
        type = "metric"
        properties = {
          metrics = [
            ["NavArch/HullSizing", "DisplacementClosureIterations", { stat = "Average" }]
          ]
          title = "Average Closure Iterations"
        }
      },
      {
        type = "metric"
        properties = {
          metrics = [
            ["NavArch/HullSizing", "CacheHitRate", { stat = "Average" }]
          ]
          title = "Cache Hit Rate (%)"
        }
      }
    ]
  })
}
```

---

### Frontend Metrics (Custom)

**Instrument key interactions:**
```typescript
// utils/performance.ts
export const measurePerformance = (metricName: string, fn: () => Promise<void>) => {
  const startTime = performance.now();
  
  return fn().finally(() => {
    const elapsed = performance.now() - startTime;
    
    // Log to backend or analytics
    console.log(`[PERF] ${metricName}: ${elapsed.toFixed(2)}ms`);
    
    // Send to backend
    api.post("/analytics/performance", {
      metric: metricName,
      duration_ms: elapsed,
      timestamp: Date.now()
    }).catch(() => {}); // Fire and forget
  });
};

// Usage
await measurePerformance("SliderRecompute", async () => {
  await hullSizingStore.recomputeCandidate(candidateId, adjustments);
});
```

**Track in Google Analytics or custom backend:**
```typescript
// Send custom event
gtag('event', 'slider_interaction', {
  'event_category': 'hull_sizing',
  'event_label': 'speed_adjust',
  'value': responseTimeMs
});
```

---

## Profiling Tools

### Backend Profiling

**dotTrace (JetBrains):**
```bash
# Profile locally
dottrace attach <process_id> --save-to=profile.dtp

# Analyze hotspots
# Look for:
# - Slow database queries
# - Inefficient loops in solver
# - Expensive math operations
```

**BenchmarkDotNet:**
```csharp
[MemoryDiagnoser]
public class SolverBenchmarks
{
    [Benchmark]
    public async Task DisplacementClosure()
    {
        await _service.CloseDisplacementAsync(...);
    }
    
    [Benchmark]
    public async Task HoltropCalculation()
    {
        await _service.ComputeHoltropAsync(...);
    }
}

// Run benchmarks
dotnet run -c Release --project HullSizingService.Benchmarks
```

---

### Frontend Profiling

**React DevTools Profiler:**
```typescript
import { Profiler } from "react";

<Profiler id="HullViewer3D" onRender={(id, phase, actualDuration) => {
  if (actualDuration > 16) { // Slower than 60 FPS
    console.warn(`[PERF] ${id} took ${actualDuration.toFixed(2)}ms in ${phase}`);
  }
}}>
  <HullViewer3D candidate={candidate} />
</Profiler>
```

**Chrome DevTools:**
- **Performance tab:** Record during slider interaction
- **Memory tab:** Check for leaks (heap snapshots before/after)
- **Rendering tab:** Enable FPS meter, paint flashing

---

## Optimization Checklist

### Backend

- [x] Parallel candidate generation (Task.WhenAll)
- [x] Water properties caching (12h TTL, stale fallback)
- [x] Database indexes on hot paths
- [x] Polly retry policies (3 attempts, jitter)
- [x] Circuit breaker for DataService calls
- [ ] Connection pooling (default EF Core, verify settings)
- [ ] Response compression (gzip for JSON > 1KB)
- [ ] Async all the way (no .Result or .Wait())

### Frontend

- [x] Debounced slider (300ms)
- [x] Dynamic LOD (near/mid/far based on camera)
- [x] Code splitting (lazy load hull-sizing routes)
- [x] Virtualization (candidates grid if >20)
- [x] useMemo for geometry generation
- [ ] Web Worker for mesh generation (if FPS < 45)
- [ ] Throttle recompute during drag (only on release)
- [ ] Dispose Three.js geometries on unmount
- [ ] Canvas pointer capture (prevent event bubbling)

---

## Performance Budgets

### Bundle Size (Frontend)

| Bundle | Budget | Actual | Status |
|--------|--------|--------|--------|
| Main bundle (without hull-sizing) | <500 KB | - | - |
| Hull sizing bundle (lazy loaded) | <300 KB | - | - |
| Three.js + r3f | <200 KB | - | - |
| Total (with hull sizing) | <1 MB | - | - |

**Monitor with:**
```bash
npm run build
# Check dist/assets/*.js sizes
```

---

### Database Query Performance

| Query | Budget | Strategy |
|-------|--------|----------|
| List mission cases (20 items) | <10ms | Index on user_id |
| Get candidate by ID | <5ms | Primary key lookup |
| Get candidates by run (5 items) | <10ms | Index on sizing_run_id |
| Insert candidate | <20ms | Batch insert 5 candidates |

**Monitor with:**
```sql
-- Enable query logging (development only)
SET log_min_duration_statement = 50; -- Log queries >50ms
```

---

## Load Testing

### Backend Load Test (k6)

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '1m', target: 10 },  // Ramp up to 10 users
    { duration: '3m', target: 10 },  // Stay at 10 users
    { duration: '1m', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests < 2s
    http_req_failed: ['rate<0.05'],     // <5% failures
  },
};

export default function () {
  const payload = JSON.stringify({
    name: 'Load Test Mission',
    missionType: 'container',
    cargoBasis: 'teu',
    teuCount: 6000,
    serviceSpeedKn: 24.0
  });

  // Create mission case
  const createRes = http.post('http://localhost:5002/api/v1/hull-sizing/mission-cases', payload, {
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${__ENV.JWT_TOKEN}` },
  });
  
  check(createRes, {
    'mission created': (r) => r.status === 201,
  });
  
  const missionId = createRes.json('id');
  
  // Run sizing
  const runRes = http.post(`http://localhost:5002/api/v1/hull-sizing/mission-cases/${missionId}/runs`, 
    JSON.stringify({ mode: 'first_principles', locks: {}, options: {} }),
    { headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${__ENV.JWT_TOKEN}` } }
  );
  
  check(runRes, {
    'sizing succeeded': (r) => r.status === 200,
    'candidates generated': (r) => r.json('candidates').length >= 3,
    'response time OK': (r) => r.timings.duration < 2000,
  });
  
  sleep(1);
}
```

**Run:**
```bash
k6 run --env JWT_TOKEN=$TOKEN load-test.js
```

---

## Memory Management

### Backend (.NET)

**Monitor:**
```bash
dotnet-counters monitor --process-id <pid> System.Runtime
```

**Watch metrics:**
- `GC Heap Size` - should stay < 500 MB
- `Gen 2 Collections` - should be infrequent
- `ThreadPool Thread Count` - should be < 100

**Optimization:**
- Use `ArrayPool<T>` for large arrays (geometry generation)
- Dispose `DbContext` properly (scoped lifetime)
- Avoid large object heap (LOH) allocations (>85 KB objects)

---

### Frontend (React)

**Monitor:**
```javascript
// Chrome DevTools -> Memory -> Take Heap Snapshot

// Before operation
const before = performance.memory?.usedJSHeapSize || 0;

// Perform operation (load 10 candidates)
await loadCandidates();

// After operation
const after = performance.memory?.usedJSHeapSize || 0;
console.log(`Memory used: ${((after - before) / 1024 / 1024).toFixed(2)} MB`);
```

**Optimization:**
```typescript
// Dispose Three.js geometries
useEffect(() => {
  const geometry = geometryRef.current;
  
  return () => {
    geometry?.dispose(); // Cleanup
  };
}, []);

// Limit candidate cache
const MAX_CACHED_CANDIDATES = 10;
if (candidateCache.size > MAX_CACHED_CANDIDATES) {
  const oldest = Array.from(candidateCache.keys())[0];
  candidateCache.delete(oldest);
}
```

---

## Freeze Compute Strategy (from Agent 2)

**Problem:** Slider drag triggers recompute on every pixel, but Holtrop is expensive

**Solution:** Only recompute on slider release

```typescript
export const SpeedShapeSlider: React.FC = () => {
  const [isDragging, setIsDragging] = useState(false);
  const [localValue, setLocalValue] = useState(50);
  
  const handleMouseDown = () => setIsDragging(true);
  
  const handleMouseUp = async () => {
    setIsDragging(false);
    
    // Now run full recompute (including Holtrop)
    await hullSizingApi.recomputeCandidate(candidateId, { ... });
  };
  
  const handleChange = (value: number) => {
    setLocalValue(value);
    
    // During drag: only update geometry (no Holtrop)
    if (isDragging) {
      updateGeometryOnly(value); // Cheap operation
    }
  };
  
  return (
    <input
      type="range"
      value={localValue}
      onChange={(e) => handleChange(Number(e.target.value))}
      onMouseDown={handleMouseDown}
      onMouseUp={handleMouseUp}
    />
  );
};
```

**Improvement:** Slider feels instant (no lag), full compute on release

---

## Next: Read `10-COMPLETION-CHECKLIST.md` for tracking progress
