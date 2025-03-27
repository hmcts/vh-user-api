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

            // Act
            LoggerAdapter.LogInformation(_mockLogger.Object, message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<string>(),
                    null,
                    It.IsAny<Func<string, Exception, string>>()),
                Times.Once);

        }

        [Test]
        public void LogWarning_ShouldLogWarningMessage()
        {
            // Arrange
            const string message = "Test warning message";

            // Act
            LoggerAdapter.LogWarning(_mockLogger.Object, message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<string>(),
                    null,
                    It.IsAny<Func<string, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void LogError_ShouldLogErrorMessage()
        {
            // Arrange
            const string message = "Test error message";

            // Act
            LoggerAdapter.LogError(_mockLogger.Object, message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<string>(),
                    null,
                    It.IsAny<Func<string, Exception?, string>>()),
                Times.Once);
        }
    }
}
