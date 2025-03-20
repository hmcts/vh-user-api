namespace UserApi.Common.Logging
{
    using System;
    using Microsoft.Extensions.Logging;

    public static partial class LoggerAdapter
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "{Message}")]
        public static partial void LogInformation(this ILogger logger, string message);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "`{Message}`")]
        public static partial void LogWarning(this ILogger logger, string message);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Error,
            Message = "`{Message}`")]    
        public static partial void LogError(this ILogger logger, string message);
    }
}