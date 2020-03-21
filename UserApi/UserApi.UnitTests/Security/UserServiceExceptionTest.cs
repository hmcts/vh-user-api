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
            Assert.AreEqual(reason, exception.Reason);
            Assert.AreEqual($"{message}: {reason}", exception.Message);
        }

        [Test]
        public void Should_return_empty_reson_message()
        {
            const string reason = "";
            const string message = "";

            var exception = new UserServiceException(message, reason);
            Assert.AreEqual(reason, exception.Reason);
            Assert.AreEqual(": ", exception.Message);
        }
    }
}
