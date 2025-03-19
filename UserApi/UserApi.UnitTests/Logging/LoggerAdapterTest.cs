namespace UserApi.UnitTests.Logging
{
    using System;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using UserApi.Common.Logging;

    public class LoggerAdapterTest
    {
        private Mock<ILogger<LoggerAdapterTest>> _mockLogger;
        private LoggerAdapter<LoggerAdapterTest> _loggerAdapter;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<LoggerAdapterTest>>();
            _loggerAdapter = new LoggerAdapter<LoggerAdapterTest>(_mockLogger.Object);
        }

        [Test]
        public void LogError_Should_LogError_WithCorrectArguments()
        {
            // Arrange
            var message = "Error with arguments {0} and {1}";
            var args = new object[] { "arg1", "arg2" };
            _mockLogger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);

            // Act
            _loggerAdapter.LogError(message, args);

            // Assert
            _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString() == string.Format(message, args)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void LogInformation_Should_LogInformation_WithCorrectArguments()
        {
            // Arrange
            var message = "Information with arguments {0} and {1}";
            var args = new object[] { "arg1", "arg2" };
            _mockLogger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

            // Act
            _loggerAdapter.LogInformation(message, args);

            // Assert
            _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString() == string.Format(message, args)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void LogWarning_Should_LogWarning_WithCorrectArguments()
        {
            // Arrange
            var message = "Warning with arguments {0} and {1}";
            var args = new object[] { "arg1", "arg2" };
            _mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

            // Act
            _loggerAdapter.LogWarning(message, args);

            // Assert
            _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString() == string.Format(message, args)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void LogError_Should_NotThrowException_When_MessageIsNull()
        {
            // Arrange
            string message = null;
            _mockLogger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);

            // Act & Assert
            Assert.DoesNotThrow(() => _loggerAdapter.LogError(message));
        }

        [Test]
        public void LogInformation_Should_NotThrowException_When_MessageIsNull()
        {
            // Arrange
            string message = null;
            _mockLogger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);

            // Act & Assert
            Assert.DoesNotThrow(() => _loggerAdapter.LogInformation(message));
        }

        [Test]
        public void LogWarning_Should_NotThrowException_When_MessageIsNull()
        {
            // Arrange
            string message = null;
            _mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

            // Act & Assert
            Assert.DoesNotThrow(() => _loggerAdapter.LogWarning(message));
        }
    }
}