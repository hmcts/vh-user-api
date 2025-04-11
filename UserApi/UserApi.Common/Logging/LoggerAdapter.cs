namespace UserApi.Common.Logging
{
    using System;
    using Microsoft.Extensions.Logging;

    public static partial class LoggerAdapter
    {
        [LoggerMessage(
            EventId = 5000, 
            Level = LogLevel.Error,
            Message = "CreateUser validation failed: {errors}")]
        public static partial void LogErrorCreateUserValidation(this ILogger logger, string errors);
        
        [LoggerMessage(
            EventId = 5001, 
            Level = LogLevel.Error,
            Message = "Update User Account validation failed: {errors}")]
        public static partial void LogErrorUpdateUserAccount(this ILogger logger, string errors);
        [LoggerMessage(
            EventId = 5002, 
            Level = LogLevel.Error,
            Message = "Add User To Group validation failed: {errors}")]
        public static partial void LogErrorAddUserToGroupvalidation(this ILogger logger, string errors);
        [LoggerMessage(
            EventId = 5003, 
            Level = LogLevel.Error,
            Message = "Group not found: {groupName}")]
        public static partial void LogErrorGroupNotFound(this ILogger logger, string groupName);

    }
}