# Testing Strategy

## Overview
Comprehensive testing across unit, integration, E2E, performance, and acceptance levels.

**Coverage Target:** >80% for solver services, >60% overall

---

## Unit Tests (Backend)

### Test Project Setup
```bash
cd backend
dotnet new xunit -n HullSizingService.Tests
dotnet add HullSizingService.Tests reference HullSizingService
dotnet add HullSizingService.Tests reference Shared
dotnet add HullSizingService.Tests package Moq
dotnet add HullSizingService.Tests package FluentAssertions
dotnet sln add HullSizingService.Tests
```

### Solver Tests (`Services/Solvers/`)

#### DisplacementClosureServiceTests.cs

```csharp
public class DisplacementClosureServiceTests
{
    private readonly DisplacementClosureService _service;
    
    [Theory]
    [InlineData(58000, 230.0, 6.5, 2.5, 1.35, 0.65)] // Container
    [InlineData(300000, 320.0, 5.5, 3.0, 1.45, 0.82)] // Tanker
    public async Task CloseDisplacement_ConvergesWithinTolerance(
        decimal targetDisplacement,
        decimal initialLwl,
        decimal lOverB,
        decimal bOverT,
        decimal dOverT,
        decimal cb)
    {
        // Arrange
        var mission = CreateMissionCase(targetDisplacement);
        var locks = new LocksDto { KeepFn = true };
        
        // Act
        var result = await _service.CloseDisplacementAsync(
            targetDisplacement, initialLwl, lOverB, bOverT, dOverT,
            cb, 0.67m, 0.89m, mission, locks, CancellationToken.None);
        
        // Assert
        var error = Math.Abs((result.Displacement - targetDisplacement) / targetDisplacement);
        error.Should().BeLessThan(0.01m); // ±1%
        result.Iterations.Should().BeLessThan(50);
    }
    
    [Fact]
    public async Task CloseDisplacement_RespectsKeepFnLock()
    {
        // Arrange
        var mission = CreateMissionCase(58000);
        var locks = new LocksDto { KeepFn = true };
        var initialFn = 0.26m;
        var initialLwl = CalculateLwlFromFn(mission.ServiceSpeedKn, initialFn);
        
        // Act
        var result = await _service.CloseDisplacementAsync(...);
        
        // Assert
        var finalFn = mission.ServiceSpeedKn * 0.5144m / (decimal)Math.Sqrt((double)(9.81m * result.Lwl));
        Math.Abs(finalFn - initialFn).Should().BeLessThan(0.001m); // Fn unchanged
    }
    
    [Fact]
    public async Task CloseDisplacement_EnforcesMaxDraftConstraint()
    {
        // Arrange
        var mission = CreateMissionCase(58000);
        mission.CapDraftM = 10.0m; // Strict draft limit
        
        // Act
        var result = await _service.CloseDisplacementAsync(...);
        
        // Assert
        result.T.Should().BeLessThanOrEqualTo(10.0m);
        result.Flags.Should().Contain("draft_exceeded");
    }
}
```

---

#### HoltropResistanceServiceTests.cs

```csharp
public class HoltropResistanceServiceTests
{
    private readonly HoltropResistanceService _service;
    
    [Fact]
    public async Task ComputeHoltrop_MatchesKCSReferenceValues()
    {
        // Arrange - KCS hull at design speed
        var lpp = 230.0m;
        var lwl = 232.5m;
        var b = 32.2m;
        var t = 10.8m;
        var cb = 0.651m;
        var cp = 0.670m;
        var cwp = 0.890m;
        var speedMs = 12.35m; // 24 kn
        var rho = 1025m;
        var nu = 1.19e-6m;
        
        // KCS reference: EHP ≈ 20,500 kW at 24 kn (published data)
        
        // Act
        var result = await _service.ComputeHoltropAsync(
            lpp, lwl, b, t, cb, cp, cwp, speedMs, rho, nu, CancellationToken.None);
        
        var ehp = result.TotalResistanceN * speedMs / 1000m;
        
        // Assert (allow ±10% for simplified formulation)
        ehp.Should().BeInRange(18450, 22550); // 20,500 ± 10%
    }
    
    [Theory]
    [InlineData(0.60, 0.001450)] // CB=0.60, expected Cf
    [InlineData(0.70, 0.001450)] // CB=0.70, expected Cf (same Rn)
    public async Task ComputeHoltrop_ITTC57FrictionCorrect(decimal cb, decimal expectedCf)
    {
        // Arrange
        var rn = 1e9m; // Fixed Reynolds number
        
        // Act
        var cf = ComputeITTC57Friction(rn);
        
        // Assert
        cf.Should().BeApproximately(expectedCf, 0.000010m);
    }
}
```

---

#### StabilityScreenServiceTests.cs

```csharp
public class StabilityScreenServiceTests
{
    [Fact]
    public async Task ComputeQuickGM_BargeMatchesAnalytical()
    {
        // Arrange - Rectangular barge (analytical solution available)
        var lwl = 100m;
        var b = 20m;
        var t = 5m;
        var cwp = 1.0m; // Rectangle
        var kg = 3.0m; // Assumed
        
        // Analytical: 
        // Iwp = L·B³/12 = 100·20³/12 = 66,667 m⁴
        // ∇ = L·B·T = 100·20·5 = 10,000 m³
        // BMt = Iwp/∇ = 66,667/10,000 = 6.67 m
        // KB ≈ T/2 = 2.5 m (rectangular section)
        // GMt = KB + BMt - KG = 2.5 + 6.67 - 3.0 = 6.17 m
        
        // Act
        var result = await _service.ComputeQuickGMAsync(lwl, b, t, cwp, "barge", CancellationToken.None);
        
        // Assert
        result.BMt.Should().BeApproximately(6.67m, 0.1m);
        result.KB.Should().BeApproximately(2.5m, 0.2m);
        result.GMt.Should().BeApproximately(6.17m, 0.3m);
    }
}
```

---

### Controller Tests

#### MissionCasesControllerTests.cs

```csharp
public class MissionCasesControllerTests
{
    private readonly Mock<IMissionCaseService> _mockService;
    private readonly MissionCasesController _controller;
    
    [Fact]
    public async Task Create_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var dto = new CreateMissionCaseDto { Name = "", MissionType = "invalid" };
        
        // Act
        var result = await _controller.Create(dto, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Fact]
    public async Task Create_ReturnsForbidden_WhenTenantIdMissing()
    {
        // Arrange
        _controller.HttpContext.Items.Remove("TenantId");
        var dto = new CreateMissionCaseDto { ... };
        
        // Act
        var result = await _controller.Create(dto, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }
    
    [Fact]
    public async Task Delete_SoftDeletesRecord()
    {
        // Arrange
        var id = Guid.NewGuid();
        
        // Act
        await _controller.Delete(id, CancellationToken.None);
        
        // Assert
        _mockService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## Integration Tests (Backend)

### Setup
```csharp
public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;
    
    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
}
```

### End-to-End Sizing Workflow Test

```csharp
public class SizingWorkflowIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task FullWorkflow_CreateMission_RunSizing_PushToHydrostatics()
    {
        // 1. Create mission case
        var createDto = new CreateMissionCaseDto
        {
            Name = "Test Container",
            MissionType = "container",
            CargoBasis = "teu",
            TeuCount = 6000,
            ServiceSpeedKn = 24.0
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/hull-sizing/mission-cases", createDto);
        createResponse.Should().HaveStatusCode(HttpStatusCode.Created);
        var missionCase = await createResponse.Content.ReadFromJsonAsync<MissionCaseDto>();
        
        // 2. Run sizing
        var runDto = new CreateSizingRunDto
        {
            Mode = "first_principles",
            Locks = new LocksDto { KeepFn = true }
        };
        
        var runResponse = await _client.PostAsJsonAsync(
            $"/api/v1/hull-sizing/mission-cases/{missionCase.Id}/runs", runDto);
        runResponse.Should().HaveStatusCode(HttpStatusCode.OK);
        var result = await runResponse.Content.ReadFromJsonAsync<SizingResultDto>();
        
        result.Candidates.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Candidates.First().Rank.Should().Be(1);
        
        // 3. Verify displacement closure
        var bestCandidate = result.Candidates.First();
        var displacementError = Math.Abs((bestCandidate.DisplacementT - 58000) / 58000);
        displacementError.Should().BeLessThan(0.01m); // ±1%
        
        // 4. Push to Hydrostatics (mock DataService response)
        var pushDto = new PushToHydrostaticsRequestDto { VesselName = "Test Vessel" };
        var pushResponse = await _client.PostAsJsonAsync(
            $"/api/v1/hull-sizing/candidates/{bestCandidate.Id}/push-to-hydrostatics",
            pushDto,
            new Dictionary<string, string> { ["X-Idempotency-Key"] = "test-key-123" });
        
        pushResponse.Should().HaveStatusCode(HttpStatusCode.Created);
        var pushResult = await pushResponse.Content.ReadFromJsonAsync<PushToHydrostaticsResultDto>();
        pushResult.VesselId.Should().NotBeEmpty();
    }
}
```

---

## Frontend Tests (Jest + React Testing Library)

### Component Tests

#### MissionInputForm.test.tsx

```typescript
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MissionInputForm } from "../MissionInputForm";

describe("MissionInputForm", () => {
  it("validates required fields", async () => {
    render(<MissionInputForm onSubmit={jest.fn()} />);
    
    const submitButton = screen.getByText("Compute");
    fireEvent.click(submitButton);
    
    await waitFor(() => {
      expect(screen.getByText("Mission name required")).toBeInTheDocument();
      expect(screen.getByText("Mission type required")).toBeInTheDocument();
    });
  });
  
  it("shows volume input when volume basis selected", () => {
    render(<MissionInputForm onSubmit={jest.fn()} />);
    
    const volumeRadio = screen.getByLabelText("Volume (m³)");
    fireEvent.click(volumeRadio);
    
    expect(screen.getByLabelText("Cargo Volume (m³)")).toBeInTheDocument();
    expect(screen.getByLabelText("Cargo Density (t/m³)")).toBeInTheDocument();
  });
  
  it("shows TEU input when TEU basis selected", () => {
    render(<MissionInputForm onSubmit={jest.fn()} />);
    
    const teuRadio = screen.getByLabelText("TEU");
    fireEvent.click(teuRadio);
    
    expect(screen.getByLabelText("TEU Count")).toBeInTheDocument();
  });
});
```

---

#### CandidateCard.test.tsx

```typescript
describe("CandidateCard", () => {
  const mockCandidate: CandidateDesign = {
    id: "cand-1",
    rank: 1,
    hullFamily: "container",
    lppM: 232.5,
    bM: 32.5,
    tM: 11.2,
    fn: 0.26,
    displacementT: 58200,
    ehpKw: 21500,
    score: 0.92,
    flagsJson: {},
    geometryJson: {...}
  };
  
  it("displays key metrics correctly", () => {
    render(<CandidateCard candidate={mockCandidate} onSelect={jest.fn()} />);
    
    expect(screen.getByText(/232\.5 m/)).toBeInTheDocument(); // Lpp
    expect(screen.getByText(/32\.5 m/)).toBeInTheDocument(); // B
    expect(screen.getByText(/0\.260/)).toBeInTheDocument(); // Fn
    expect(screen.getByText(/92\.0%/)).toBeInTheDocument(); // Score
  });
  
  it("shows flags when constraints violated", () => {
    const candidateWithFlags = {...mockCandidate, flagsJson: {draft_exceeded: true, low_gm: true}};
    render(<CandidateCard candidate={candidateWithFlags} onSelect={jest.fn()} />);
    
    expect(screen.getByText("draft_exceeded")).toBeInTheDocument();
    expect(screen.getByText("low_gm")).toBeInTheDocument();
  });
});
```

---

### Store Tests

#### HullSizingStore.test.ts

```typescript
import { hullSizingStore } from "../../../stores/HullSizingStore";
import { hullSizingApi } from "../../../api/hull-sizing-api";

jest.mock("../../../api/hull-sizing-api");

describe("HullSizingStore", () => {
  beforeEach(() => {
    hullSizingStore.reset();
  });
  
  it("creates mission case and updates store", async () => {
    const mockResponse = { id: "mission-123", name: "Test Mission", ... };
    (hullSizingApi.createMissionCase as jest.Mock).mockResolvedValue(mockResponse);
    
    await hullSizingStore.createMissionCase({name: "Test Mission", ...});
    
    expect(hullSizingStore.currentMissionCase).toEqual(mockResponse);
    expect(hullSizingStore.loading).toBe(false);
  });
  
  it("runs sizing and populates candidates", async () => {
    const mockResult = {
      run: {id: "run-123"},
      candidates: [{id: "cand-1", rank: 1}, {id: "cand-2", rank: 2}]
    };
    (hullSizingApi.createRun as jest.Mock).mockResolvedValue(mockResult);
    
    await hullSizingStore.runSizing("mission-123", {}, {});
    
    expect(hullSizingStore.candidates).toHaveLength(2);
    expect(hullSizingStore.candidates[0].rank).toBe(1);
  });
});
```

---

## E2E Tests (Cypress)

### Test Scenarios

#### hull-sizing-wizard.spec.ts

```typescript
describe("Hull Sizing Wizard", () => {
  beforeEach(() => {
    cy.login(); // Custom command for auth
    cy.visit("/hull-sizing");
  });
  
  it("completes full wizard flow and generates candidates", () => {
    // Step 1: Mission & Cargo
    cy.get('[data-testid="mission-name"]').type("6000 TEU Feeder");
    cy.get('[data-testid="mission-type"]').select("container");
    cy.get('[data-testid="cargo-basis-teu"]').click();
    cy.get('[data-testid="teu-count"]').type("6000");
    
    // Step 2: Speed & Environment
    cy.get('[data-testid="service-speed"]').type("24");
    cy.get('[data-testid="env-hs"]').type("3.5");
    cy.get('[data-testid="env-tz"]').type("8.0");
    
    // Step 3: Constraints
    cy.get('[data-testid="max-draft"]').type("12");
    
    // Step 4: Compute
    cy.get('[data-testid="compute-button"]').click();
    
    // Wait for results
    cy.get('[data-testid="candidate-card"]', { timeout: 10000 })
      .should("have.length.at.least", 3);
    
    // Verify first candidate has valid data
    cy.get('[data-testid="candidate-card"]').first().within(() => {
      cy.contains(/Lpp:/);
      cy.contains(/Score:/);
    });
  });
});
```

---

#### hull-sizing-slider.spec.ts

```typescript
describe("Hull Sizing Slider Interaction", () => {
  it("adjusts hull with slider and updates within 300ms", () => {
    cy.login();
    cy.visit("/hull-sizing/workspace/run-123");
    
    // Record initial metrics
    cy.get('[data-testid="lpp-value"]').invoke("text").as("initialLpp");
    
    // Drag slider
    const startTime = Date.now();
    cy.get('[data-testid="speed-shape-slider"]')
      .invoke("val", 70)
      .trigger("change");
    
    // Wait for update
    cy.get('[data-testid="lpp-value"]').should(($el) => {
      const elapsed = Date.now() - startTime;
      expect(elapsed).to.be.lessThan(300); // <300ms
      expect($el.text()).to.not.equal(this.initialLpp);
    });
  });
});
```

---

## Performance Tests

### Backend Performance Benchmarks

```csharp
[Fact]
public async Task SolverPerformance_GeneratesCandidatesUnder2Seconds()
{
    // Arrange
    var mission = CreateTypicalContainerMission();
    var options = new SizingOptions();
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    var candidates = await _solver.SolveAsync(mission, options, CancellationToken.None);
    stopwatch.Stop();
    
    // Assert
    candidates.Should().HaveCountGreaterThanOrEqualTo(3);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // <2s
}

[Fact]
public async Task DisplacementClosure_ConvergesUnder50ms()
{
    var stopwatch = Stopwatch.StartNew();
    var result = await _closureService.CloseDisplacementAsync(...);
    stopwatch.Stop();
    
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
}
```

---

### Frontend Performance Tests

#### Measure Slider Response Time

```typescript
it("slider response time < 300ms", async () => {
  const startTime = performance.now();
  
  // Simulate slider drag
  await userEvent.drag(slider, { delta: { x: 100 } });
  
  // Wait for API response + re-render
  await waitFor(() => {
    expect(screen.getByTestId("lpp-value")).toHaveTextContent(/235/);
  });
  
  const elapsed = performance.now() - startTime;
  expect(elapsed).toBeLessThan(300);
});
```

#### Measure 3D FPS

```typescript
it("3D rendering maintains ≥45 FPS", () => {
  render(<HullViewer3D candidate={mockCandidate} />);
  
  // Use Stats from @react-three/drei to measure FPS
  const fpsReadings: number[] = [];
  
  // Simulate camera movement for 2 seconds
  // Record FPS every 100ms
  
  const avgFps = fpsReadings.reduce((a, b) => a + b) / fpsReadings.length;
  expect(avgFps).toBeGreaterThanOrEqual(45);
});
```

---

## Reference Test Cases (Acceptance Criteria)

### From test_matrix.csv

#### FP_Container_Base

```
Mission:
  Type: container
  Cargo: 58,000 t (weight basis)
  Speed: 24.0 kn
  Sea margin: 15%

Expected Results:
  Lpp: 230 - 250 m
  B: 32 - 36 m
  T: 10 - 12 m
  Cb: 0.60 - 0.70
  Fn: 0.23 - 0.30
  Δ error: ≤ 1%

Acceptance:
  ✓ Δ closes within ±1%
  ✓ Fn within band (0.23-0.30)
  ✓ Draft ≤ 12 m (if constrained)
  ✓ No solver failures
  ✓ Generates ≥3 candidates
```

---

#### FP_Tanker_Base

```
Mission:
  Type: tanker
  Cargo: 300,000 t (weight basis)
  Speed: 16.0 kn
  Sea margin: 15%

Expected Results:
  Lpp: 320 - 340 m
  B: 58 - 62 m
  T: 20 - 22 m
  Cb: 0.80 - 0.85
  Fn: 0.12 - 0.18
  Δ error: ≤ 1%

Acceptance:
  ✓ Δ closes within ±1%
  ✓ Fn within band (0.12-0.18)
  ✓ Draft ≤ 21 m (if constrained)
  ✓ No solver failures
```

---

#### FP_Fishing_Base

```
Mission:
  Type: fishing
  Cargo: 1,000 t (weight basis)
  Speed: 12.0 kn

Expected Results:
  Lpp: 45 - 55 m
  B: 9 - 11 m
  T: 4 - 5 m
  Cb: 0.55 - 0.65
  Fn: 0.18 - 0.28

Acceptance:
  ✓ Δ closes within ±1%
  ✓ Fn within band
  ✓ Generates ≥3 candidates
```

---

## Automated Test Execution

### Backend Tests

```bash
# Run all tests
dotnet test backend/HullSizingService.Tests

# Run with coverage
dotnet test backend/HullSizingService.Tests /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage

# Run specific test
dotnet test backend/HullSizingService.Tests --filter "FullyQualifiedName~DisplacementClosureServiceTests"
```

---

### Frontend Tests

```bash
# Run all tests
cd frontend
npm test

# Run with coverage
npm test -- --coverage

# Run E2E tests
npm run test:e2e
```

---

## Coverage Report

### Target Coverage by Module

| Module | Coverage Target | Priority |
|--------|----------------|----------|
| Solvers (DisplacementClosure, Holtrop, Stability) | >80% | Critical |
| Services (MissionCase, SizingRun, Candidate) | >70% | High |
| Controllers | >60% | Medium |
| Seed/Migration | >40% | Low |
| **Overall Backend** | **>60%** | - |
| **Frontend Components** | **>50%** | - |

---

## CI/CD Test Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/hull-sizing-ci.yml
name: HullSizingService CI

on:
  pull_request:
    paths:
      - 'backend/HullSizingService/**'
  push:
    branches: [main, develop]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore backend/HullSizingService
      
      - name: Build
        run: dotnet build backend/HullSizingService --no-restore
      
      - name: Test
        run: dotnet test backend/HullSizingService.Tests --no-build --verbosity normal
      
      - name: Format Check
        run: dotnet format backend/HullSizingService --verify-no-changes
```

---

## Next: Read `08-DEVOPS-CICD.md` for infrastructure plan
