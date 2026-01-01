using CharonDbContext.Data;
using CharonDbContext.Models;
using CharonGateway.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CharonGateway.IntegrationTests.Controllers;

public class MetricsControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public MetricsControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Use the same DbContext from the application's DI container
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        // In-memory database is ready to use
        // Each test will add its own data
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        _scope.Dispose();
        _client.Dispose();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        // Clear any existing data first
        _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
        await _dbContext.SaveChangesAsync();
        
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/metrics?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MetricDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnBadRequest_WhenPageIsInvalid()
    {
        // Act
        var response = await _client.GetAsync("/api/metrics?page=0&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Act
        var response = await _client.GetAsync("/api/metrics?page=1&pageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMetric_ShouldReturnNotFound_WhenMetricDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/metrics/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMetric_ShouldReturnOk_WhenMetricExists()
    {
        // Arrange
        // Clear any existing data first
        _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
        await _dbContext.SaveChangesAsync();
        
        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = JsonSerializer.Serialize(new Dictionary<string, object> { { "motionDetected", true } }),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Metrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/metrics/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MetricDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Type.Should().Be("motion");
    }

    [Fact]
    public async Task GetMetricsByType_ShouldReturnFilteredMetrics()
    {
        // Arrange
        // Clear any existing data first
        _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
        await _dbContext.SaveChangesAsync();
        
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/metrics/type/motion");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MetricDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.Type == "motion");
    }

    [Fact]
    public async Task GetMetricsAggregation_ShouldReturnAggregation()
    {
        // Arrange
        // Clear any existing data first
        _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
        await _dbContext.SaveChangesAsync();
        
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/metrics/aggregation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MetricsAggregationDto>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(3);
        result.TypeAggregations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMetricTypes_ShouldReturnDistinctTypes()
    {
        // Arrange
        // Clear any existing data first
        _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
        await _dbContext.SaveChangesAsync();
        
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/metrics/types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<string>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().Contain("motion");
        result.Should().Contain("energy");
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/metrics/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

