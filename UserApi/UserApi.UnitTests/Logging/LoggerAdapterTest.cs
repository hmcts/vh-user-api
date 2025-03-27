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
        public void LogInformation_ShouldLogInformationMessage()
        {
            // Arrange
            const string message = "Test information message";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });

            // Act
            _mockLogger.Object.LogInformation(message);

           
        }

        [Test]
        public void LogWarning_ShouldLogWarningMessage()
        {
            // Arrange
            const string message = "Test warning message";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });

            _mockLogger.Object.LogWarning(message);
        }

        [Test]
        public void LogError_ShouldLogErrorMessage()
        {
            // Arrange
            const string message = "Test error message";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

            _mockLogger
                .Setup(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<Func<object, Exception?, string>>()))
                .Callback<LogLevel, EventId, object, Exception?, Delegate>((_, _, state, _, _) =>
                {
                    //Assert
                    Assert.That(state!.ToString(), Does.Contain(message));
                });
            
            // Act
            _mockLogger.Object.LogError(message);
        }
    }
}
