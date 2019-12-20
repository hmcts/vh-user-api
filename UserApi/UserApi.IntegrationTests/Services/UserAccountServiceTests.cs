using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common;
using Testing.Common.ActiveDirectory;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class UserAccountServiceTests
    {
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
        public async Task should_generate_username_based_on_firstname_lastname()
        {
            var nextUsername = await _service.CheckForNextAvailableUsernameAsync("Missing", "User");
            nextUsername.Should().Be("missing.user@hearings.reform.hmcts.net");
        }

        [Test]
        public async Task should_get_next_available_username_for_firstname_lastname()
        {
            var nextUsername = await _service.CheckForNextAvailableUsernameAsync("Existing", "Individual");
            nextUsername.Should().Be("existing.individual1@hearings.reform.hmcts.net");
        }

        [Test]
        public async Task should_create_user()
        {
            const string firstName = "Automatically";
            const string lastName = "Created";
            var unique = DateTime.Now.ToString("yyyyMMddhmmss");
            var recoveryEmail = $"{firstName}.{lastName}.{unique}@hearings.hmcts.net";
            var createdAccount = await _service.CreateUserAsync(firstName, lastName, recoveryEmail);
            var username = createdAccount.Username;
            username.ToLower().Should().Contain(firstName.ToLower());
            username.ToLower().Should().Contain(lastName.ToLower());

            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(username, "Need real token from TokenProvider");
        }
    }
}
