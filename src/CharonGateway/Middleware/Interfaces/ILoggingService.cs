namespace CharonGateway.Middleware.Interfaces;

public interface ILoggingService
{
    void LogMethodStart(string methodName);
    void LogMethodSuccess(string methodName, object? result = null, TimeSpan? duration = null);
    void LogMethodFailure(string methodName, Exception exception, TimeSpan? duration = null);
}


