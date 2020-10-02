using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common.ActiveDirectory;
using Testing.Common.Configuration;
using UserApi.Caching;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class UserAccountServiceTests
    {
        private UserAccountService _service;
        private GraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _identityServiceApiClient;
        private Mock<ICache> _distributedCache;
        private IPasswordService _passwordService;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();

            var settings = TestConfig.Instance.Settings;
            var tokenProvider = new TokenProvider(TestConfig.Instance.AzureAd);
            _graphApiSettings = new GraphApiSettings(tokenProvider, TestConfig.Instance.AzureAd);
            _passwordService = new PasswordService();
            _identityServiceApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings, _passwordService, settings);
            _distributedCache = new Mock<ICache>();
            _service = new UserAccountService(_secureHttpRequest, _graphApiSettings, _identityServiceApiClient, settings, _distributedCache.Object);
        }

        [Test]
        public async Task should_generate_username_based_on_firstname_lastname()
        {
            var nextUsername = await _service.CheckForNextAvailableUsernameAsync("Missing", "User");
            nextUsername.Should().Be($"missing.user@{TestConfig.Instance.Settings.ReformEmail}");
        }

        [Test]
        public async Task should_get_next_available_username_for_firstname_lastname()
        {
         
            var nextUsername = await _service.CheckForNextAvailableUsernameAsync("Existing", "Individual");
            nextUsername.Should().Be($"existing.individual@{TestConfig.Instance.Settings.ReformEmail}");
        }

        [Test]
        public async Task should_create_user()
        {
            const string firstName = "Automatically";
            const string lastName = "Created";
            var unique = DateTime.Now.ToString("yyyyMMddhmmss");
            var recoveryEmail = $"{firstName}.{lastName}.{unique}@{TestConfig.Instance.Settings.ReformEmail}";
            var createdAccount = await _service.CreateUserAsync(firstName, lastName, recoveryEmail, false);
            var username = createdAccount.Username;
            username.ToLower().Should().Contain(firstName.ToLower());
            username.ToLower().Should().Contain(lastName.ToLower());

            await ActiveDirectoryUser.DeleteTheUserFromAdAsync(username, _graphApiSettings.AccessToken);
        }

        [Test]
        public void should_throw_exception_when_attempting_to_delete_nonexistent_user()
        {
            const string username = "does.notexist@anywhere.com";
            var result = Assert.ThrowsAsync<UserDoesNotExistException>(() => _service.DeleteUserAsync(username));
            result.Username.Should().Be(username);
        }
    }
}
