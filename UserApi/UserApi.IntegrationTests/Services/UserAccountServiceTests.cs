using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testing.Common;
using Testing.Common.ActiveDirectory;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class UserAccountServiceTests
    {
        private UserAccountService _service;
        private OptionsWrapper<AzureAdConfiguration> _configuration;
        private GraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _identityServiceApiClient;
        private OptionsWrapper<Settings> _settings;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();
            
            _configuration = new OptionsWrapper<AzureAdConfiguration>(TestConfig.Instance.AzureAd);
            _settings = new OptionsWrapper<Settings>(TestConfig.Instance.Settings);
            var tokenProvider = new TokenProvider(_configuration);
            _graphApiSettings = new GraphApiSettings(tokenProvider, _configuration);
            _identityServiceApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings, _settings);
            _service = new UserAccountService(_secureHttpRequest, _graphApiSettings, _identityServiceApiClient);
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