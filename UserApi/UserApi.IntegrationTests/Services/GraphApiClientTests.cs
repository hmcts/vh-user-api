using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.IntegrationTests.Services
{
    public class GraphApiClientTests
    {
        private OptionsWrapper<AzureAdConfiguration> _configuration;
        private GraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _graphApiClient;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();
            
            _configuration = new OptionsWrapper<AzureAdConfiguration>(TestConfig.Instance.AzureAd);
            _graphApiSettings = new GraphApiSettings(new TokenProvider(_configuration), _configuration);
            var settings = new OptionsWrapper<Settings>(new Settings());
            _graphApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings, settings);
        }

        [Test]
        public async Task should_return_all_users_beginning_with_filter()
        {
            var users = await _graphApiClient.GetUsernamesStartingWith("automation");
            foreach (var username in users)
            {
                username.ToLower().Should().StartWith("automation");
            }
        }
    }
}