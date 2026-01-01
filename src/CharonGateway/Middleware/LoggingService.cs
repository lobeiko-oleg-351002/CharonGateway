using CharonGateway.Middleware.Interfaces;

namespace CharonGateway.Middleware;

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogMethodStart(string methodName)
    {
        _logger.LogInformation("Starting {MethodName}", methodName);
    }

    public void LogMethodSuccess(string methodName, object? result = null, TimeSpan? duration = null)
    {
        if (duration.HasValue)
        {
            _logger.LogInformation(
                "Completed {MethodName} successfully in {Duration}ms",
                methodName,
                duration.Value.TotalMilliseconds);
        }
        else
        {
            _logger.LogInformation("Completed {MethodName} successfully", methodName);
        }
    }

    public void LogMethodFailure(string methodName, Exception exception, TimeSpan? duration = null)
    {
        if (duration.HasValue)
        {
            _logger.LogError(
                exception,
                "Failed {MethodName} after {Duration}ms",
                methodName,
                duration.Value.TotalMilliseconds);
        }
        else
        {
            _logger.LogError(exception, "Failed {MethodName}", methodName);
        }
    }
}


