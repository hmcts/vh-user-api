using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common;
using Testing.Common.ActiveDirectory;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using Group = Microsoft.Graph.Group;

namespace UserApi.IntegrationTests.Services
{
    public class UserAccountServiceTests
    {
        private UserAccountService _service;
        private GraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _identityServiceApiClient;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();

            var settings = TestConfig.Instance.Settings;
            var tokenProvider = new TokenProvider(TestConfig.Instance.AzureAd);
            _graphApiSettings = new GraphApiSettings(tokenProvider, TestConfig.Instance.AzureAd);
            _identityServiceApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings, settings);
            _service = new UserAccountService(_secureHttpRequest, _graphApiSettings, _identityServiceApiClient, settings);
        }

        [Test]
        public async Task should_generate_username_based_on_firstname_lastname()
        {
            var nextUsername = await _service.CheckForNextAvailableUsername("Missing", "User");
            nextUsername.Should().Be("missing.user@hearings.reform.hmcts.net");
        }

        [Test]
        public async Task should_get_next_available_username_for_firstname_lastname()
        {
            var nextUsername = await _service.CheckForNextAvailableUsername("Existing", "Individual");
            nextUsername.Should().Be("existing.individual1@hearings.reform.hmcts.net");
        }

        [Test]
        public async Task should_throw_exception_trying_to_add_user_to_invalid_group()
        {
            var user = await _service.GetUserById("Automation01Professional01@hearings.reform.hmcts.net");
            Assert.ThrowsAsync<UserServiceException>(() => _service.AddUserToGroup(user, new Group {Id = "invalid"}));
        }

        [Test]
        public async Task should_create_user()
        {
            const string firstName = "Automatically";
            const string lastName = "Created";
            var unique = DateTime.Now.ToString("yyyyMMddhmmss");
            var recoveryEmail = $"{firstName}.{lastName}.{unique}@hearings.hmcts.net";
            var createdAccount = await _service.CreateUser(firstName, lastName, recoveryEmail);
            var username = createdAccount.Username;
            username.ToLower().Should().Contain(firstName.ToLower());
            username.ToLower().Should().Contain(lastName.ToLower());

            await ActiveDirectoryUser.DeleteTheUserFromAd(username, _graphApiSettings.AccessToken);
        }
    }
}