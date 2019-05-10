using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class ActiveDirectoryUserAccountServiceTests
    {
        private ActiveDirectoryUserAccountService _service;

        [SetUp]
        public void Setup()
        {
            var secureHttpRequest = new SecureHttpRequest();
            
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();
            
            var azureAdConfig = new AzureAdConfiguration();
            configRootBuilder.Build().GetSection("AzureAd").Bind(azureAdConfig);
            
            var configuration = new OptionsWrapper<AzureAdConfiguration>(azureAdConfig);
            var tokenProvider = new TokenProvider(configuration);
            var graphApiSettings = new GraphApiSettings(tokenProvider, configuration);
            _service = new ActiveDirectoryUserAccountService(secureHttpRequest, graphApiSettings);
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