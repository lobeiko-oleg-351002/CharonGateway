using Microsoft.AspNetCore.SignalR;
using CharonDbContext.Models;
using System.Text.Json;

namespace CharonGateway.Hubs;

public class MetricsHub : Hub
{
    public async Task SendMetric(Metric metric)
    {
        // Convert Metric to the format expected by the frontend
        var metricDto = new
        {
            id = metric.Id,
            type = metric.Type,
            name = metric.Name,
            payload = string.IsNullOrEmpty(metric.PayloadJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) ?? new Dictionary<string, object>(),
            createdAt = metric.CreatedAt.ToString("O") // ISO 8601 format
        };

        await Clients.All.SendAsync("MetricReceived", metricDto);
    }
}

