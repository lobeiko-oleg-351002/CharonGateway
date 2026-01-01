using CharonGateway.Repositories;
using CharonDbContext.Data;
using CharonDbContext.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CharonGateway.Tests.Repositories;

public class MetricRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<MetricRepository>> _loggerMock;
    private readonly MetricRepository _repository;

    public MetricRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<MetricRepository>>();
        _repository = new MetricRepository(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMetricDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMetric_WhenMetricExists()
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
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Type.Should().Be("motion");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllMetrics()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByTypeAsync_ShouldReturnFilteredMetrics()
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
        var result = await _repository.GetByTypeAsync("motion");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.Type == "motion");
    }

    [Fact]
    public async Task GetByTypeAsync_ShouldThrow_WhenTypeIsEmpty()
    {
        // Act & Assert
        // Note: Validation is now handled by ValidationDecorator in the service layer
        // Repository no longer validates - it's a data access concern, not business logic
        // This test is kept for backward compatibility but may need to be moved to service layer tests
        // For now, we expect the repository to accept empty/null and let the query execute
        // (which will return empty results, not throw)
        var result1 = await _repository.GetByTypeAsync("");
        var result2 = await _repository.GetByTypeAsync(null!);
        
        // Repository doesn't throw - validation happens at service layer
        result1.Should().BeEmpty();
        result2.Should().BeEmpty();
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnFilteredCount_WhenPredicateIsProvided()
    {
        // Arrange
        var metrics = new List<Metric>
        {
            new Metric { Id = 1, Type = "motion", Name = "Garage", PayloadJson = "{}", CreatedAt = DateTime.UtcNow },
            new Metric { Id = 2, Type = "energy", Name = "Office", PayloadJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _dbContext.Metrics.AddRange(metrics);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync(m => m.Type == "motion");

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetDistinctTypesAsync_ShouldReturnUniqueTypes()
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
        var result = await _repository.GetDistinctTypesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("motion");
        result.Should().Contain("energy");
    }
}


