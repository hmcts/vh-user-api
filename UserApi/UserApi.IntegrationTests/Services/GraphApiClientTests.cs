using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common.Configuration;
using UserApi.Common.Configuration;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class GraphApiClientTests
    {
        private GraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _graphApiClient;
        private IPasswordService _passwordService;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();

            var config = TestConfig.Instance.AzureAd;
            var settings = TestConfig.Instance.Settings;
            _graphApiSettings = new GraphApiSettings(new TokenProvider(config), config);
            _passwordService = new PasswordService();
            _graphApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings, _passwordService, settings);
        }

        [Test]
        public async Task should_return_all_users_beginning_with_filter()
        {
            var users = await _graphApiClient.GetUsernamesStartingWithAsync("automation");
            foreach (var username in users)
            {
                username.ToLower().Should().StartWith("automation");
            }
        }

        [Test]
        public async Task GetUsernamesStartingWith_should_check_deleted_users_when_contact_email_is_provided()
        {
            var users = await _graphApiClient.GetUsernamesStartingWithAsync("automation", "auto@test.com");
            foreach (var username in users)
            {
                username.ToLower().Should().StartWith("automation");
            }
        }
        
        [Test]
        public async Task GetUsernamesStartingWith_should_check_deleted_users_when_name_is_provided()
        {
            var users = await _graphApiClient.GetUsernamesStartingWithAsync("automation", null, "automation", "test");
            foreach (var username in users)
            {
                username.ToLower().Should().StartWith("automation");
            }
        }
    }
}