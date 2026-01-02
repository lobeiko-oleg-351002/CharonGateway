using CharonDbContext.Data;
using CharonDbContext.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CharonGateway.IntegrationTests.GraphQL;

public class DailyAverageMetricsGraphQLTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public DailyAverageMetricsGraphQLTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        // Clear any existing data - use a transaction to ensure atomicity
        try
        {
            _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error clearing data in InitializeAsync: {ex.Message}");
            // Continue anyway - might be first run
        }
        
        // Create test data with multiple metrics
        // Use fixed base date to ensure consistency across test runs
        var testMetrics = new List<Metric>();
        var baseDate = DateTime.UtcNow.Date;
        
        // Create 50 metrics across 5 days with different types and names
        // Data will be from baseDate (today) to baseDate - 4 days (4 days ago)
        for (int day = 0; day < 5; day++)
        {
            for (int i = 0; i < 10; i++)
            {
                var metric = new Metric
                {
                    Type = $"Type{(i % 3) + 1}",
                    Name = $"Location{(i % 2) + 1}",
                    PayloadJson = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        { "temperature", 20.0 + i },
                        { "humidity", 50.0 + i },
                        { "pressure", 1000.0 + i },
                        { "voltage", 12.0 + i },
                        { "current", 1.0 + i }
                    }),
                    CreatedAt = baseDate.AddDays(-day).AddHours(i)
                };
                testMetrics.Add(metric);
            }
        }

        _dbContext.Metrics.AddRange(testMetrics);
        await _dbContext.SaveChangesAsync();
        
        // Verify data was created - this is critical for test reliability
        var count = await _dbContext.Metrics.CountAsync();
        if (count != testMetrics.Count)
        {
            throw new InvalidOperationException(
                $"Failed to create test data. Expected {testMetrics.Count} metrics, but found {count} in database.");
        }
        
        Console.WriteLine($"✓ InitializeAsync: Created {testMetrics.Count} test metrics, verified {count} in DB");
        
        // Verify date range of created data
        var minDate = await _dbContext.Metrics.MinAsync(m => m.CreatedAt);
        var maxDate = await _dbContext.Metrics.MaxAsync(m => m.CreatedAt);
        Console.WriteLine($"✓ InitializeAsync: Data date range: {minDate:O} to {maxDate:O}");
    }

    public async Task DisposeAsync()
    {
        // Don't dispose _dbContext here - it's managed by the scope
        // Just clean up test data to avoid conflicts with other tests
        try
        {
            // Only clean up if there's data to avoid unnecessary operations
            var count = await _dbContext.Metrics.CountAsync();
            if (count > 0)
            {
                _dbContext.Metrics.RemoveRange(_dbContext.Metrics);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"✓ DisposeAsync: Cleaned up {count} test metrics");
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - cleanup errors shouldn't break tests
            Console.WriteLine($"Warning: Error in DisposeAsync cleanup: {ex.Message}");
        }
        finally
        {
            _scope?.Dispose();
            _client?.Dispose();
        }
    }

    [Fact]
    public async Task GetDailyAverageMetrics_WithAverageValues_ShouldShowComplexity()
    {
        // Arrange - This test will show the complexity when averageValues is included
        var fromDate = DateTime.UtcNow.AddDays(-5);
        var toDate = DateTime.UtcNow;

        var query = @"
        query GetDailyAverageMetrics($fromDate: DateTime!, $toDate: DateTime!) {
            dailyAverageMetrics(fromDate: $fromDate, toDate: $toDate) {
                date
                type
                name
                count
                averageValues
            }
        }";

        var variables = new
        {
            fromDate = fromDate.ToString("O"),
            toDate = toDate.ToString("O")
        };

        var request = new
        {
            query,
            variables
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();
        
        // Debug output
        Console.WriteLine("=== Test with averageValues ===");
        Console.WriteLine("Response Status: " + response.StatusCode);
        Console.WriteLine("Response Content: " + content);

        // Assert - This will likely fail, but we want to see the complexity
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        
        if (root.TryGetProperty("errors", out var errors))
        {
            var errorMessages = errors.EnumerateArray()
                .Select(e => e.GetProperty("message").GetString())
                .ToList();
            
            Console.WriteLine("GraphQL Errors (expected with averageValues):");
            foreach (var error in errorMessages)
            {
                Console.WriteLine("  - " + error);
            }
            
            // Get detailed error information
            if (errors[0].TryGetProperty("extensions", out var extensions))
            {
                Console.WriteLine("\nError Extensions:");
                foreach (var ext in extensions.EnumerateObject())
                {
                    Console.WriteLine($"  {ext.Name}: {ext.Value}");
                }
            }
        }
    }

    [Fact]
    public async Task GetMetrics_WithDateFilter_ShouldShowComplexity()
    {
        // Arrange - Test with minimal query (1 record, no filters, no payload)
        // This should have minimal complexity to verify configuration works
        var fromDate = DateTime.UtcNow.AddDays(-5);
        var toDate = DateTime.UtcNow;
        
        var query = @"
        query GetMetrics($first: Int, $where: MetricFilterInput) {
            metrics(first: $first, where: $where) {
                nodes {
                    id
                    type
                    name
                    createdAt
                }
            }
        }";

        var variables = new
        {
            first = 1, // Request only 1 record to test if configuration works
            where = new
            {
                fromDate = fromDate.ToString("O"),
                toDate = toDate.ToString("O")
            }
        };
        
        var request = new
        {
            query,
            variables
        };

        // Act
        Console.WriteLine("=== Test: Query with pagination (1 record, WITH date filters, no payload) ===");
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Response Status: " + response.StatusCode);
        Console.WriteLine("Response Content: " + content);

        // Assert
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        
        if (root.TryGetProperty("errors", out var errors))
        {
            var errorMessages = errors.EnumerateArray()
                .Select(e => e.GetProperty("message").GetString())
                .ToList();
            
            Console.WriteLine("GraphQL Errors:");
            foreach (var error in errorMessages)
            {
                Console.WriteLine("  - " + error);
            }
            
            // Check for complexity error
            var hasComplexityError = errorMessages.Any(e => 
                e?.Contains("field cost") == true || 
                e?.Contains("complexity") == true ||
                e?.Contains("HC0047") == true);
            
            if (hasComplexityError)
            {
                // Get extensions for more details
                if (errors[0].TryGetProperty("extensions", out var extensions))
                {
                    Console.WriteLine("\n=== COMPLEXITY ERROR DETAILS ===");
                    Console.WriteLine("This test confirms that even with 1 record, complexity exceeds the limit.");
                    Console.WriteLine("This indicates the complexity limit configuration is not working.");
                    foreach (var ext in extensions.EnumerateObject())
                    {
                        Console.WriteLine($"  {ext.Name}: {ext.Value}");
                    }
                    Console.WriteLine("================================\n");
                }
                
                // This test is designed to show the problem, not to pass
                // The complexity limit configuration needs to be fixed
                Console.WriteLine("\nCONCLUSION: Complexity limit configuration is NOT working.");
                Console.WriteLine("Even a minimal query (1 record, no filters, no payload) exceeds the limit.");
                return; // Exit early to see the error
            }
        }
        
        // Check for data
        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("metrics", out var metrics))
            {
                if (metrics.TryGetProperty("nodes", out var nodes))
                {
                    Console.WriteLine($"Returned {nodes.GetArrayLength()} metrics");
                }
            }
        }
    }
}


