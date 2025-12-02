# Hull Sizing Validation Test Suite

This directory contains the comprehensive validation test suite for hull sizing, validating results against real-world test scenarios from the Ship Design Validation Handbook.

## Directory Structure

```
Validation/
├── Unit/              # Fast, isolated unit tests (run in all CI/CD pipelines)
├── Integration/       # Full pipeline integration tests (run in full test suite)
└── README.md         # This file
```

## Test Categories

### Unit Tests (`Unit/`)
- **Purpose**: Test validation logic in isolation with mock data
- **Speed**: Fast (< 1 second total execution time)
- **Execution**: Run in all CI/CD pipelines
- **Trait**: `[Trait("Category", "Unit")]`
- **Filter**: `dotnet test --filter "Category=Unit"`

### Integration Tests (`Integration/`)
- **Purpose**: Test full pipeline with real hull generation
- **Speed**: Slower (5-30 seconds per test)
- **Execution**: Run in full test suite, can be skipped in PR checks
- **Trait**: `[Trait("Category", "Integration")]`
- **Filter**: `dotnet test --filter "Category=Integration"`

### Long Running Tests
- **Purpose**: Very slow tests (>30 seconds)
- **Execution**: Run only in nightly builds
- **Trait**: `[Trait("Category", "LongRunning")]`
- **Filter**: `dotnet test --filter "Category=LongRunning"`

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests Only (Fast)
```bash
dotnet test --filter "Category=Unit"
```

### Run Integration Tests
```bash
dotnet test --filter "Category=Integration"
```

### Skip Long Running Tests
```bash
dotnet test --filter "Category!=LongRunning"
```

### Run Fast Tests (Unit + Integration, exclude Long Running)
```bash
dotnet test --filter "Category!=LongRunning"
```

## Test Data

All validation test cases are defined in:
- `TestData/ValidationTestCases.cs` - Single source of truth for test cases
- `TestData/ResistanceCoefficientReference.cs` - Resistance validation data
- `Shared/TestData/AlexanderLimitReference.cs` - Alexander Limit curve data

## Adding New Tests

### Unit Test Example
```csharp
[Fact]
[Trait("Category", "Unit")]
public void ValidateAlexanderLimit_BelowLimit_ReturnsInfo()
{
    // Arrange
    var fn = 0.20m;
    var cb = 0.65m;
    
    // Act
    var severity = AlexanderLimitReference.GetSeverityLevel(fn, cb);
    
    // Assert
    severity.Should().Be("Info");
}
```

### Integration Test Example
```csharp
[Fact]
[Trait("Category", "Integration")]
[Trait("Category", "LongRunning")]
public async Task CalibrationCase_FullPipeline_ValidatesCorrectly()
{
    // Full pipeline test here...
}
```

## Test Coverage

- **Target**: >85% code coverage on validation services
- **Focus**: Edge cases, boundary values, error paths
- **Validation**: Use code coverage reports to identify gaps

## Documentation

See `.plan/app-docs/hull-sizing/validation/` for detailed implementation plans for each phase.

## Test Cases

- **Calibration Case**: 40,000 DWT Product Carrier (Gold Standard)
- **TC-A**: Bulk Carrier/VLCC (250,000t, 15kn)
- **TC-B**: General Cargo (50,000t, 20kn)
- **TC-B-Slow**: General Cargo at 12 knots (slower speed optimization)
- **TC-C**: Fast Container Ship (10,000t, 25kn)

