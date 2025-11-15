# DataService.Tests

**Comprehensive test suite for DataService**

## Overview

This test project contains unit tests, integration tests, and performance tests for the DataService, which handles hydrostatics, resistance, seakeeping, and catalog functionality.

## Test Organization

```
DataService.Tests/
├── Controllers/              # Controller tests (API endpoints)
├── Services/                 # Service layer unit tests
│   ├── Hydrostatics/        # Hydrostatic calculations
│   ├── Resistance/          # Resistance calculations
│   ├── Seakeeping/          # Seakeeping analysis
│   └── Catalog/             # Catalog services
├── Integration/             # Integration tests (API, DB)
├── Performance/             # Performance benchmarks
├── Helpers/                 # Test utilities
│   └── TestDataGenerator.cs # Test data creation
└── TestData/                # Reference data for validation

```

## Running Tests

### All Tests

```bash
dotnet test
```

### Specific Test Category

```bash
# Unit tests only (default)
dotnet test --filter "Category!=Integration&Category!=Performance"

# Integration tests
dotnet test --filter "Category=Integration"

# Performance tests
dotnet test --filter "Category=Performance"
```

### Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~HydroCalculatorTests"
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Categories

### Unit Tests (Services/)

- **Fast** (< 100ms per test)
- **Isolated** (mocked dependencies)
- **High coverage** (80%+ target)

**Examples:**
- `HydroCalculatorTests.cs` - Hydrostatic calculation algorithms
- `HoltropResistanceServiceTests.cs` - Resistance calculations
- `StripTheoryEngineTests.cs` - Seakeeping strip theory

### Integration Tests (Integration/)

- **Medium speed** (1-5s per test)
- **Real database** (TestContainers)
- **API contracts** (full request/response)

**Examples:**
- `ApiIntegrationTests.cs` - API endpoint validation
- `SeedDataIntegrationTests.cs` - Database seeding

### Performance Tests (Performance/)

- **Benchmarks** (measure execution time)
- **Regression detection** (compare against baselines)
- **Algorithm optimization** (identify bottlenecks)

**Examples:**
- `AlgorithmPerformanceTests.cs` - Computational performance

## Test Data

### Reference Cases

The test suite includes reference data for validation:

1. **Wigley Hull** - Analytical solution (parabolic waterlines)
2. **Rectangular Barge** - Hand-calculated values
3. **Series 60** - ITTC benchmark data

### Test Data Generator

Use `TestDataGenerator` to create test vessels:

```csharp
using DataService.Tests.Helpers;

// Create rectangular barge (analytical properties)
var barge = TestDataGenerator.CreateRectangularBarge();

// Create Wigley hull (benchmark)
var wigley = TestDataGenerator.CreateWigleyHull();

// Create cargo ship
var cargo = TestDataGenerator.CreateCargoShip();

// Create test fleet (5 vessels)
var fleet = TestDataGenerator.CreateTestFleet(5);

// Create loadcase
var loadcase = TestDataGenerator.CreateLoadcase(vesselId: 1);
```

## Best Practices

### Writing Tests

✅ **Use descriptive names:**

```csharp
[Fact]
public void ComputeDisplacement_RectangularBarge_ReturnsAnalyticalValue()
{
    // Given a rectangular barge
    // When computing displacement
    // Then return analytical value
}
```

✅ **Use FluentAssertions:**

```csharp
result.Displacement.Should().BeApproximately(10250, 10);
result.KB.Should().BeGreaterThan(0);
```

✅ **Mock dependencies:**

```csharp
var mockLogger = Mock.Of<ILogger<HydroCalculator>>();
var calculator = new HydroCalculator(integrationEngine, mockLogger);
```

✅ **Clean up resources:**

```csharp
public void Dispose()
{
    // Clean up test data
    _dbContext.Database.EnsureDeleted();
}
```

### Test Data

✅ **Use test data generator:**

```csharp
var vessel = TestDataGenerator.CreateRectangularBarge();
```

❌ **Don't hardcode test data:**

```csharp
// BAD: Hardcoded, not reusable
var vessel = new Vessel { Name = "Test", LengthOverall = 100, /* ... */ };
```

### Assertions

✅ **Use appropriate tolerance:**

```csharp
// Displacement should be accurate to ±10 tonnes
result.Displacement.Should().BeApproximately(expectedValue, 10);

// Form coefficients should be accurate to ±1%
result.Cb.Should().BeApproximately(0.65, 0.01);
```

## Troubleshooting

### Tests failing locally

```bash
# Restore dependencies
dotnet restore

# Clean and rebuild
dotnet clean && dotnet build

# Run with verbose output
dotnet test --verbosity normal
```

### Integration tests failing

```bash
# Ensure Docker is running
docker ps

# Check PostgreSQL container
docker run -d --name postgres-test \
  -e POSTGRES_DB=navarch_test \
  -e POSTGRES_USER=testuser \
  -e POSTGRES_PASSWORD=testpass \
  -p 5433:5432 \
  postgres:16-alpine
```

### Performance tests inconsistent

- Run on consistent hardware
- Close other applications
- Run multiple times
- Check for background processes

## Code Coverage

### View Coverage Report

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Open report
start coverage-report/index.html  # Windows
open coverage-report/index.html   # macOS
```

### Coverage Targets

- **Overall:** 90% line coverage, 80% branch coverage
- **Critical algorithms:** 100% coverage
- **Controllers:** 80% coverage
- **Services:** 90% coverage

## CI/CD Integration

### Pull Request Checks

Tests run automatically on every PR:

```yaml
- name: Run tests
  run: dotnet test --verbosity normal
```

### Manual Comprehensive Testing

Trigger via GitHub Actions:

1. Go to **Actions** tab
2. Select **Comprehensive Test Suite**
3. Click **Run workflow**

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq](https://github.com/moq/moq4)
- [TestContainers](https://dotnet.testcontainers.org/)
- [Test Execution Guide](../../temp/TEST_EXECUTION_GUIDE.md)

---

**Maintained by:** Engineering Team  
**Last Updated:** November 8, 2025










