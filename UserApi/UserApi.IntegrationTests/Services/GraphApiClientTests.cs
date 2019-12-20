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
        private IGraphApiSettings _graphApiSettings;
        private SecureHttpRequest _secureHttpRequest;
        private GraphApiClient _graphApiClient;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new SecureHttpRequest();
            
            var config = TestConfig.Instance.AzureAd;
            _graphApiSettings = new GraphApiSettings(new TokenProvider(config), config);
            _graphApiClient = new GraphApiClient(_secureHttpRequest, _graphApiSettings);
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