using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CharonGateway.Hubs;
using CharonDbContext.Models;
using CharonDbContext.Data;
using Microsoft.EntityFrameworkCore;

namespace CharonGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IHubContext<MetricsHub> _hubContext;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        IHubContext<MetricsHub> hubContext,
        ApplicationDbContext dbContext,
        ILogger<NotificationController> logger)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("metric/{id}")]
    public async Task<IActionResult> NotifyMetric(int id)
    {
        try
        {
            var metric = await _dbContext.Metrics
                .FirstOrDefaultAsync(m => m.Id == id);

            if (metric == null)
            {
                _logger.LogWarning("Metric with id {MetricId} not found", id);
                return NotFound();
            }

            // Convert Metric to the format expected by the frontend
            var metricDto = new
            {
                id = metric.Id,
                type = metric.Type,
                name = metric.Name,
                payload = string.IsNullOrEmpty(metric.PayloadJson)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(metric.PayloadJson) 
                        ?? new Dictionary<string, object>(),
                createdAt = metric.CreatedAt.ToString("O") // ISO 8601 format
            };

            await _hubContext.Clients.All.SendAsync("MetricReceived", metricDto);
            _logger.LogInformation("Metric {MetricId} notification sent to SignalR clients", id);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending metric notification for id {MetricId}", id);
            return StatusCode(500, "Error sending notification");
        }
    }
}

