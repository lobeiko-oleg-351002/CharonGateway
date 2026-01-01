using CharonGateway.Models.DTOs;
using CharonGateway.Models.Requests;
using CharonGateway.Repositories;
using CharonGateway.Repositories.Interfaces;
using CharonGateway.Services;
using CharonGateway.Services.Decorators;
using CharonGateway.Services.Interfaces;
using CharonDbContext.Data;
using CharonDbContext.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CharonGateway.Tests.Services;

public class MetricServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMetricRepository _repository;
    private readonly MetricService _service;
    private readonly IMetricService _serviceWithValidation;

    public MetricServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new MetricRepository(_dbContext, new Mock<ILogger<MetricRepository>>().Object);
        _service = new MetricService(_repository);
        _serviceWithValidation = new ValidationDecorator(_service);
    }

    [Fact]
    public async Task GetMetricByIdAsync_ShouldReturnNull_WhenMetricDoesNotExist()
    {
        // Act
        var result = await _service.GetMetricByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMetricByIdAsync_ShouldReturnDto_WhenMetricExists()
    {
        // Arrange
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
        var result = await _service.GetMetricByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Type.Should().Be("motion");
        result.Name.Should().Be("Garage");
        result.Payload.Should().ContainKey("motionDetected");
    }

    [Fact]
    public async Task GetMetricByIdAsync_ShouldThrow_WhenIdIsInvalid()
    {
        // Act & Assert - validation happens in decorator
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricByIdAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricByIdAsync(-1));
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnPagedResult_WhenRequestIsValid()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        var request = new MetricQueryRequest
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetMetricsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldFilterByType_WhenTypeIsProvided()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        var request = new MetricQueryRequest
        {
            Type = "motion",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetMetricsAsync(request);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(m => m.Type == "motion");
    }

    [Fact]
    public async Task GetMetricsByTypeAsync_ShouldReturnFilteredMetrics()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetMetricsByTypeAsync("motion", 1, 10);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.Type == "motion");
    }

    [Fact]
    public async Task GetMetricsByTypeAsync_ShouldThrow_WhenTypeIsEmpty()
    {
        // Act & Assert - validation happens in decorator
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricsByTypeAsync("", 1, 10));
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricsByTypeAsync(null!, 1, 10));
    }

    [Fact]
    public async Task GetMetricsByTypeAsync_ShouldThrow_WhenPageIsInvalid()
    {
        // Act & Assert - validation happens in decorator
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricsByTypeAsync("motion", 0, 10));
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricsByTypeAsync("motion", -1, 10));
    }

    [Fact]
    public async Task GetMetricsByTypeAsync_ShouldThrow_WhenPageSizeIsInvalid()
    {
        // Act & Assert - validation happens in decorator
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricsByTypeAsync("motion", 1, 0));
        await Assert.ThrowsAsync<ArgumentException>(() => _serviceWithValidation.GetMetricsByTypeAsync("motion", 1, 101));
    }

    [Fact]
    public async Task GetMetricsAggregationAsync_ShouldReturnAggregation()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        var request = new MetricAggregationRequest();

        // Act
        var result = await _service.GetMetricsAggregationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.TypeAggregations.Should().HaveCount(2);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "motion" && ta.Count == 2);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "energy" && ta.Count == 1);
    }

    [Fact]
    public async Task GetMetricTypesAsync_ShouldReturnDistinctTypes()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "air_quality", Name = "Kitchen", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetMetricTypesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("motion");
        result.Should().Contain("energy");
        result.Should().Contain("air_quality");
    }
}

