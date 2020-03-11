using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Testing.Common;
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

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();
            
            var config = TestConfig.Instance.AzureAd;
            var settings = TestConfig.Instance.Settings;
            _graphApiSettings = new GraphApiSettings(new TokenProvider(config), config);
            _graphApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings, settings);
        }

        [Test]
        public async Task Should_return_all_users_beginning_with_filter()
        {
            var users = await _graphApiClient.GetUsernamesStartingWithAsync("automation");
            foreach (var username in users)
            {
                username.ToLower().Should().StartWith("automation");
            }
        }
    }
}