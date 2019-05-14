using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testing.Common;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class ActiveDirectoryUserAccountServiceTests
    {
        private UserAccountService _service;
        private OptionsWrapper<AzureAdConfiguration> _configuration;
        private GraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _identityServiceApiClient;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();
            
            _configuration = new OptionsWrapper<AzureAdConfiguration>(TestConfig.Instance.AzureAd);
            var settings = new OptionsWrapper<Settings>(TestConfig.Instance.Settings);
            var tokenProvider = new TokenProvider(_configuration);
            _graphApiSettings = new GraphApiSettings(tokenProvider, _configuration);
            _identityServiceApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings);
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
    }
}