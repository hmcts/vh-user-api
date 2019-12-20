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

        private UserAccountService _service;
        private Mock<IGraphServiceClient> _graphServiceClient;

        [SetUp]
        public void Setup()
        {
            var settings = TestConfig.Instance.Settings;
            _graphServiceClient = new Mock<IGraphServiceClient>();
            
            _service = new UserAccountService(settings, _graphServiceClient.Object);
        }

        [Test]
        public async Task should_increment_the_username()
        {
            // TODO mock the graphClient and setup
            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync("Existing", "User");
            Assert.AreEqual("existing.user2" + Domain, nextAvailable);
        }
    }
}
