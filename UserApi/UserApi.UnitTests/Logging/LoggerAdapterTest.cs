using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using UserApi.Common.Logging;

namespace UserApi.Tests.Logging
{
    [TestFixture]
    public class LoggerAdapterTests
    {
        private Mock<ILogger> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Test]
        public void LogError_ShouldLogErrorCreateUserValidation()
        {
            // Arrange
            const string message = "CreateUser validation failed: error1, error2";
            const string errors = "error1, error2";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });
            
            // Act
            _mockLogger.Object.LogErrorCreateUserValidation(errors);
        }

        [Test]
        public void LogError_ShouldLogErrorUpdateUserAccount()
        {
            // Arrange
            const string message = "Update User Account validation failed: error1, error2";
            const string errors = "error1, error2";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });
            
            // Act
            _mockLogger.Object.LogErrorUpdateUserAccount(errors);
        }

        [Test]
        public void LogError_LogErrorAddUserToGroupvalidation()
        {
            // Arrange
            const string message = "Add User To Group validation failed: error1, error2";
            const string errors = "error1, error2";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });
            
            // Act
            _mockLogger.Object.LogErrorAddUserToGroupvalidation(errors);
        }

        [Test]
        public void LogError_LogErrorGroupNotFound()
        {
            // Arrange
            const string message = "Group not found: {groupName} groupName";
            const string groupName = "groupName";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });
            
            // Act
            _mockLogger.Object.LogErrorGroupNotFound(groupName);
        }
    }
}
