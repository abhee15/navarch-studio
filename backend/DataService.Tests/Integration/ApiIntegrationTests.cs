using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs;
using Xunit;

namespace DataService.Tests.Integration;

/// <summary>
/// Integration tests for DataService API endpoints
/// Tests full request/response cycles with database
/// </summary>
[Trait("Category", "Integration")]
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CatalogHulls_ReturnsPagedResults()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"/api/v1/catalog/hulls?page={page}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        // Verify response structure (adjust based on your actual response type)
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(pageSize);
    }

    [Fact]
    public async Task CatalogHulls_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var minLength = 50;
        var maxLength = 100;

        // Act
        var response = await _client.GetAsync($"/api/v1/catalog/hulls?minLpp={minLength}&maxLpp={maxLength}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Benchmarks_ReturnsListOfBenchmarks()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/benchmarks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateVessel_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var vesselDto = new VesselDto
        {
            Name = "Test Vessel",
            Lpp = 100,
            Beam = 20,
            DesignDraft = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/hydrostatics/vessels", vesselDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WaterProperties_ReturnsValidData()
    {
        // Arrange
        var temperature = 15; // °C
        var salinity = 35; // ppt

        // Act
        var response = await _client.GetAsync($"/api/v1/catalog/water?temperature={temperature}&salinity={salinity}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("density").GetDecimal().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("kinematicViscosity").GetDecimal().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Propellers_ReturnsWageningenSeries()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/catalog/propellers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Wageningen"); // Assuming propellers include Wageningen B-series
    }

    [Fact]
    public async Task DefaultValues_ReturnsSystemDefaults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/defaults");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var json = JsonDocument.Parse(content);
        // Verify some default values exist
        json.RootElement.TryGetProperty("waterDensitySalt", out _).Should().BeTrue();
    }
}
