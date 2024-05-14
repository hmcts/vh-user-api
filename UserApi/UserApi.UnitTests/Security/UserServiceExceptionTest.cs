using FluentAssertions;
using NUnit.Framework;
using UserApi.Security;

namespace UserApi.UnitTests.Security
{
    public class UserServiceExceptionTest
    { 
        [Test]
        public void Should_return_reason_message()
        {
            const string reason = "reason of error";
            const string message = "message error";

            var exception = new UserServiceException(message, reason);
            exception.Reason.Should().Be(reason);
            exception.Message.Should().Be($"{message}: {reason}");
        }

        [Test]
        public void Should_return_empty_reson_message()
        {
            const string reason = "";
            const string message = "";

            var exception = new UserServiceException(message, reason);
            exception.Reason.Should().Be(reason);
            exception.Message.Should().Be(": ");
        }
    }
}
