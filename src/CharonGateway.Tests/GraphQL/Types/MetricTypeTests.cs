using CharonDbContext.Models;
using FluentAssertions;
using System.Text.Json;

namespace CharonGateway.Tests.GraphQL.Types;

/// <summary>
/// Tests for MetricType payload resolution logic.
/// Note: The Configure method is protected, so we test the payload resolution logic directly.
/// </summary>
public class MetricTypeTests
{

    [Fact]
    public void PayloadResolution_ShouldReturnEmptyDictionary_WhenPayloadJsonIsEmpty()
    {
        // Arrange
        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        // Act - Simulate the resolution logic from MetricType
        var payload = string.IsNullOrEmpty(metric.PayloadJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) ?? new Dictionary<string, object>();

        // Assert
        payload.Should().NotBeNull();
        payload.Should().BeEmpty();
    }

    [Fact]
    public void PayloadResolution_ShouldReturnEmptyDictionary_WhenPayloadJsonIsNull()
    {
        // Arrange
        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = null!,
            CreatedAt = DateTime.UtcNow
        };

        // Act - Simulate the resolution logic from MetricType
        var payload = string.IsNullOrEmpty(metric.PayloadJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) ?? new Dictionary<string, object>();

        // Assert
        payload.Should().NotBeNull();
        payload.Should().BeEmpty();
    }

    [Fact]
    public void PayloadResolution_ShouldDeserializeValidJson()
    {
        // Arrange
        var payloadData = new Dictionary<string, object>
        {
            { "temperature", 22.5 },
            { "humidity", 45.0 },
            { "motionDetected", true }
        };

        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = JsonSerializer.Serialize(payloadData),
            CreatedAt = DateTime.UtcNow
        };

        // Act - Simulate the resolution logic from MetricType
        Dictionary<string, object> payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            payload = new Dictionary<string, object>();
        }

        // Assert
        payload.Should().NotBeNull();
        payload.Should().ContainKey("temperature");
        payload.Should().ContainKey("humidity");
        payload.Should().ContainKey("motionDetected");
    }

    [Fact]
    public void PayloadResolution_ShouldReturnEmptyDictionary_WhenJsonIsInvalid()
    {
        // Arrange
        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = "invalid json {",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Simulate the resolution logic from MetricType with error handling
        Dictionary<string, object> payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            payload = new Dictionary<string, object>();
        }

        // Assert
        payload.Should().NotBeNull();
        payload.Should().BeEmpty();
    }

    [Fact]
    public void PayloadResolution_ShouldHandleComplexPayload()
    {
        // Arrange
        var payloadData = new Dictionary<string, object>
        {
            { "sensor", new Dictionary<string, object> { { "id", "sensor1" }, { "type", "PIR" } } },
            { "readings", new[] { 1, 2, 3 } },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };

        var metric = new Metric
        {
            Id = 1,
            Type = "motion",
            Name = "Garage",
            PayloadJson = JsonSerializer.Serialize(payloadData),
            CreatedAt = DateTime.UtcNow
        };

        // Act - Simulate the resolution logic from MetricType
        Dictionary<string, object> payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            payload = new Dictionary<string, object>();
        }

        // Assert
        payload.Should().NotBeNull();
        payload.Should().ContainKey("sensor");
        payload.Should().ContainKey("readings");
        payload.Should().ContainKey("timestamp");
    }
}

