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
            _mockLogger.Object.LogInformation(message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<object>(v => v != null && v.ToString().Contains(message)), // Ensure object is not null and contains the message
                    null,
                    It.IsAny<Func<object, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void LogWarning_ShouldLogWarningMessage()
        {
            // Arrange
            const string message = "Test warning message";

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

            // Act
            _mockLogger.Object.LogWarning(message);

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

            _mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

            // Act
            _mockLogger.Object.LogError(message);

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
