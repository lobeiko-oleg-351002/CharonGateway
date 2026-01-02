using CharonGateway.Models.DTOs;
using CharonGateway.Models.Requests;
using CharonGateway.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CharonGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly IMetricService _metricService;
    private readonly IValidator<MetricQueryRequest> _queryValidator;
    private readonly IValidator<MetricAggregationRequest> _aggregationValidator;

    public MetricsController(
        IMetricService metricService,
        IValidator<MetricQueryRequest> queryValidator,
        IValidator<MetricAggregationRequest> aggregationValidator)
    {
        _metricService = metricService ?? throw new ArgumentNullException(nameof(metricService));
        _queryValidator = queryValidator ?? throw new ArgumentNullException(nameof(queryValidator));
        _aggregationValidator = aggregationValidator ?? throw new ArgumentNullException(nameof(aggregationValidator));
    }

    /// <summary>
    /// Get all metrics with optional filtering, sorting, and pagination
    /// </summary>
    /// <param name="request">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of metrics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MetricDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<MetricDto>>> GetMetrics(
        [FromQuery] MetricQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _queryValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _metricService.GetMetricsAsync(request, cancellationToken);
        AddPaginationHeaders(result);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific metric by ID
    /// </summary>
    /// <param name="id">Metric ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Metric details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MetricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MetricDto>> GetMetric(
        int id,
        CancellationToken cancellationToken)
    {
        // Validation is handled by ValidationDecorator - will throw ArgumentException if invalid
        var metric = await _metricService.GetMetricByIdAsync(id, cancellationToken);

        if (metric == null)
        {
            return NotFound(new { error = $"Metric with id {id} not found" });
        }

        return Ok(metric);
    }

    /// <summary>
    /// Get metrics by type
    /// </summary>
    /// <param name="type">Metric type</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of metrics of the specified type</returns>
    [HttpGet("type/{type}")]
    [ProducesResponseType(typeof(IEnumerable<MetricDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MetricDto>>> GetMetricsByType(
        string type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Validation is handled by ValidationDecorator - will throw ArgumentException if invalid
        var metrics = await _metricService.GetMetricsByTypeAsync(type, page, pageSize, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Get metrics aggregation (counts by type)
    /// </summary>
    /// <param name="request">Aggregation query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Metrics aggregation data</returns>
    [HttpGet("aggregation")]
    [ProducesResponseType(typeof(MetricsAggregationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MetricsAggregationDto>> GetMetricsAggregation(
        [FromQuery] MetricAggregationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _aggregationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _metricService.GetMetricsAggregationAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get daily average metrics for a date range
    /// </summary>
    /// <param name="fromDate">Start date (ISO 8601 format)</param>
    /// <param name="toDate">End date (ISO 8601 format)</param>
    /// <param name="type">Optional metric type filter</param>
    /// <param name="name">Optional metric name/location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of daily average metrics</returns>
    [HttpGet("daily-averages")]
    [ProducesResponseType(typeof(IEnumerable<DailyAverageMetricDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DailyAverageMetricDto>>> GetDailyAverages(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? type = null,
        [FromQuery] string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (fromDate >= toDate)
        {
            return BadRequest(new { error = "fromDate must be before toDate" });
        }

        var result = await _metricService.GetDailyAveragesAsync(fromDate, toDate, type, name, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get available metric types
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available metric types</returns>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetMetricTypes(
        CancellationToken cancellationToken)
    {
        var types = await _metricService.GetMetricTypesAsync(cancellationToken);
        return Ok(types);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private void AddPaginationHeaders(PagedResult<MetricDto> result)
    {
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page", result.Page.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
    }
}
