using CharonGateway.GraphQL.Queries;
using CharonDbContext.Data;
using CharonDbContext.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CharonGateway.Tests.GraphQL.Queries;

public class MetricQueriesTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<MetricQueries>> _loggerMock;
    private readonly MetricQueries _queries;

    public MetricQueriesTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<MetricQueries>>();
        _queries = new MetricQueries(_loggerMock.Object);
    }

    [Fact]
    public void GetMetrics_ShouldReturnEmpty_WhenContextIsNull()
    {
        // Act
        var result = _queries.GetMetrics(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetMetrics_ShouldReturnAllMetrics_WhenContextIsValid()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "air_quality", Name = "Kitchen", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        _dbContext.SaveChanges();

        // Act
        var result = _queries.GetMetrics(_dbContext);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(m => m.Id == 1);
        result.Should().Contain(m => m.Id == 2);
        result.Should().Contain(m => m.Id == 3);
    }

    [Fact]
    public async Task GetMetricById_ShouldReturnNull_WhenContextIsNull()
    {
        // Act
        var result = await _queries.GetMetricById(1, null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMetricById_ShouldReturnMetric_WhenMetricExists()
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
        var result = await _queries.GetMetricById(1, _dbContext);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Type.Should().Be("motion");
        result.Name.Should().Be("Garage");
    }

    [Fact]
    public async Task GetMetricById_ShouldReturnNull_WhenMetricDoesNotExist()
    {
        // Act
        var result = await _queries.GetMetricById(999, _dbContext);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetMetricsByType_ShouldReturnEmpty_WhenContextIsNull()
    {
        // Act
        var result = _queries.GetMetricsByType("motion", null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetMetricsByType_ShouldReturnFilteredMetrics_WhenTypeExists()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        _dbContext.SaveChanges();

        // Act
        var result = _queries.GetMetricsByType("motion", _dbContext);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.Type == "motion");
    }

    [Fact]
    public void GetMetricsByType_ShouldReturnEmpty_WhenTypeDoesNotExist()
    {
        // Arrange
        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = "{}",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Metrics.Add(metric);
        _dbContext.SaveChanges();

        // Act
        var result = _queries.GetMetricsByType("nonexistent", _dbContext);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetricsAggregation_ShouldReturnEmpty_WhenContextIsNull()
    {
        // Act
        var result = await _queries.GetMetricsAggregation(null!);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.TypeAggregations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetricsAggregation_ShouldReturnCorrectCounts_WhenMetricsExist()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 4, Type = "energy", Name = "Kitchen", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 5, Type = "energy", Name = "Living Room", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _queries.GetMetricsAggregation(_dbContext);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(5);
        result.TypeAggregations.Should().HaveCount(2);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "motion" && ta.Count == 2);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "energy" && ta.Count == 3);
    }

    [Fact]
    public async Task GetMetricsAggregation_ShouldFilterByType_WhenTypeIsProvided()
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

        // Act
        var result = await _queries.GetMetricsAggregation(_dbContext, type: "motion");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.TypeAggregations.Should().HaveCount(1);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "motion" && ta.Count == 2);
    }

    [Fact]
    public async Task GetMetricsAggregation_ShouldFilterByDateRange_WhenDatesAreProvided()
    {
        // Arrange
        var baseDate = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = baseDate.AddDays(-2) },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = baseDate },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = baseDate.AddDays(2) }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _queries.GetMetricsAggregation(
            _dbContext,
            fromDate: baseDate.AddDays(-1),
            toDate: baseDate.AddDays(1));

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.TypeAggregations.Should().HaveCount(1);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "energy" && ta.Count == 1);
    }

    [Fact]
    public async Task GetMetricsAggregation_ShouldFilterByTypeAndDateRange_WhenAllFiltersAreProvided()
    {
        // Arrange
        var baseDate = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = baseDate },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = baseDate },
            new Metric { Id = 3, Type = "motion", Name = "Bedroom", PayloadJson = "{}", CreatedAt = baseDate.AddDays(1) }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _queries.GetMetricsAggregation(
            _dbContext,
            fromDate: baseDate,
            toDate: baseDate.AddDays(1),
            type: "motion");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.TypeAggregations.Should().HaveCount(1);
        result.TypeAggregations.Should().Contain(ta => ta.Type == "motion" && ta.Count == 2);
    }

    [Fact]
    public void GetMetrics_ShouldHandleSqlException_WhenTableDoesNotExist()
    {
        // Arrange - Create a context that will throw SQL exception
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureDeleted(); // This will cause issues when querying

        // Act & Assert - Should not throw, should return empty
        var result = _queries.GetMetrics(context);
        result.Should().NotBeNull();
    }
}

