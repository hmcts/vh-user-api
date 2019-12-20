using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common;
using UserApi.Services;

namespace UserApi.UnitTests.Services
{
    public class UserAccountServiceTests
    {
        private const string Domain = "@hearings.reform.hmcts.net";

        private Mock<IIdentityServiceApiClient> _identityServiceApiClient;
        private UserAccountService _service;
        private Mock<IGraphServiceClient> _graphServiceClient;

        [SetUp]
        public void Setup()
        {
            var settings = TestConfig.Instance.Settings;
            _identityServiceApiClient = new Mock<IIdentityServiceApiClient>();
            _graphServiceClient = new Mock<IGraphServiceClient>();
            
            _service = new UserAccountService(settings, _graphServiceClient.Object);
        }

        [Test]
        public async Task should_increment_the_username()
        {
            // given api returns
            var existingUsers = new[] {"existing.user", "existing.user1"};
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWith(It.IsAny<string>()))
                .ReturnsAsync(existingUsers.Select(username => username + Domain));

            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync("Existing", "User");
            Assert.AreEqual("existing.user2" + Domain, nextAvailable);
        }
    }
}
