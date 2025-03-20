namespace UserApi.Common.Logging
{
    using System;
    using Microsoft.Extensions.Logging;

    public partial class LoggerAdapter<T> : ILoggerAdapter<T>
    {
        private readonly ILogger<T> _logger;

        public LoggerAdapter(ILogger<T> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "`{Message} {args}`")]
        public partial void LogInformation(string message, params object[] args);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "`{Message} {args}`")]
        public partial void LogWarning(string message, params object[] args);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Error,
            Message = "`{Message} {args}`")]
        public partial void LogError(string message, params object[] args);
    }
}