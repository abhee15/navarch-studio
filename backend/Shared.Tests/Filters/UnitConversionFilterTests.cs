using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NavArch.UnitConversion.Services;
using Shared.DTOs;
using Shared.Filters;

namespace Shared.Tests.Filters;

public class UnitConversionFilterTests
{
    private readonly Mock<IUnitConverter> _mockConverter;
    private readonly Mock<ILogger<UnitConversionFilter>> _mockLogger;
    private readonly UnitConversionFilter _filter;

    public UnitConversionFilterTests()
    {
        _mockConverter = new Mock<IUnitConverter>();
        _mockLogger = new Mock<ILogger<UnitConversionFilter>>();
        _filter = new UnitConversionFilter(_mockConverter.Object, _mockLogger.Object);

        // Setup converter to return same value (no actual conversion)
        _mockConverter
            .Setup(c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((decimal value, string from, string to, string qty) => value);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithNullUnitsProperty_ShouldNotCrash()
    {
        // Arrange
        var dto = new TestDto { Value = 100m, Units = null };
        var context = CreateActionExecutingContext();
        var resultContext = CreateResultContext(context, dto);

        // Act
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));

        // Assert - Should complete without exception
        dto.Units.Should().Be("SI", "null Units should default to SI");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithEmptyUnitsProperty_ShouldNotCrash()
    {
        // Arrange
        var dto = new TestDto { Value = 100m, Units = "" };
        var context = CreateActionExecutingContext();
        var resultContext = CreateResultContext(context, dto);

        // Act
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));

        // Assert - Should complete without exception
        dto.Units.Should().Be("SI", "empty Units should default to SI");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAnonymousObject_ShouldConvertNestedDTOs()
    {
        // Arrange
        var dto = new TestDto { Value = 100m, Units = "SI" };
        var anonymousResult = new { vessels = new List<TestDto> { dto }, total = 1 };

        var context = CreateActionExecutingContext();
        context.HttpContext.Items["PreferredUnits"] = "Imperial";
        var resultContext = CreateResultContext(context, anonymousResult);

        _mockConverter
            .Setup(c => c.Convert(100m, "SI", "Imperial", "Length"))
            .Returns(328.08m);

        // Act
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));

        // Assert - Should have converted nested DTO
        dto.Value.Should().Be(328.08m, "nested DTO should be converted");
        dto.Units.Should().Be("Imperial", "Units should be updated");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithExceptionInConversion_ShouldNotCrashRequest()
    {
        // Arrange
        var dto = new TestDto { Value = 100m, Units = "SI" };
        var context = CreateActionExecutingContext();
        context.HttpContext.Items["PreferredUnits"] = "Imperial";
        var resultContext = CreateResultContext(context, dto);

        _mockConverter
            .Setup(c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Conversion failed"));

        // Act & Assert - Should complete without crashing the request
        var act = async () => await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));
        await act.Should().NotThrowAsync("filter should handle conversion errors gracefully");

        // Verify warning was logged (property-level conversion errors log as Warning, not Error)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ConvertResponseToPreferredUnits_WithString_ShouldNotIterateCharacters()
    {
        // Arrange
        var stringResult = "This is a test string";
        var context = CreateActionExecutingContext();
        context.HttpContext.Items["PreferredUnits"] = "Imperial";
        var resultContext = CreateResultContext(context, stringResult);

        // Act
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));

        // Assert - Should complete without attempting to iterate string as IEnumerable<char>
        // If it tried to iterate, it would throw or behave unexpectedly
        resultContext.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)resultContext.Result).Value.Should().Be(stringResult);
    }

    [Fact]
    public async Task ConvertResponseToPreferredUnits_WithNestedCollection_ShouldConvertAllItems()
    {
        // Arrange
        var dtos = new List<TestDto>
        {
            new TestDto { Value = 100m, Units = "SI" },
            new TestDto { Value = 200m, Units = "SI" }
        };

        var context = CreateActionExecutingContext();
        context.HttpContext.Items["PreferredUnits"] = "Imperial";
        var resultContext = CreateResultContext(context, dtos);

        _mockConverter
            .Setup(c => c.Convert(It.IsAny<decimal>(), "SI", "Imperial", "Length"))
            .Returns((decimal v, string f, string t, string q) => v * 3.28084m);

        // Act
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));

        // Assert - All items should be converted
        dtos[0].Value.Should().BeApproximately(328.084m, 0.01m);
        dtos[0].Units.Should().Be("Imperial");
        dtos[1].Value.Should().BeApproximately(656.168m, 0.01m);
        dtos[1].Units.Should().Be("Imperial");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithNon200StatusCode_ShouldNotConvert()
    {
        // Arrange
        var dto = new TestDto { Value = 100m, Units = "SI" };
        var context = CreateActionExecutingContext();
        context.HttpContext.Items["PreferredUnits"] = "Imperial";
        var resultContext = CreateResultContext(context, dto, statusCode: 400);

        // Act
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));

        // Assert - Should not convert for non-2xx responses
        dto.Value.Should().Be(100m, "should not convert on error response");
        dto.Units.Should().Be("SI", "Units should remain unchanged");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithNullResult_ShouldNotCrash()
    {
        // Arrange
        var context = CreateActionExecutingContext();
        var resultContext = CreateResultContext(context, null);

        // Act & Assert - Should complete without exception
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(resultContext));
    }

    // Helper methods
    private ActionExecutingContext CreateActionExecutingContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["PreferredUnits"] = "SI";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object()
        );
    }

    private ActionExecutedContext CreateResultContext(
        ActionExecutingContext context,
        object? value,
        int statusCode = 200)
    {
        var result = new ObjectResult(value) { StatusCode = statusCode };

        return new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            new object())
        {
            Result = result
        };
    }

    // Test DTO
    private class TestDto : UnitAwareDto
    {
        [Shared.Attributes.Convertible("Length")]
        public decimal Value { get; set; }
    }
}
