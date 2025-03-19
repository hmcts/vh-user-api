namespace UserApi.Common.Logging
{
    using Microsoft.Extensions.Logging;

    public class LoggerAdapter<T> : ILoggerAdapter<T>
    {
        private readonly ILogger<T> _logger;

        public LoggerAdapter(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogError(string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogInformation(message, args);
            }
        }
        public void LogInformation(string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(message, args);
            }
        }

        public void LogWarning(string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogInformation(message, args);
            }
        }
    }
}