using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;

namespace UserApi.UnitTests.Helpers
{
    public class GraphApiSettingsTests
    {
        private GraphApiSettings _graphApiSettings;
        private Mock<ITokenProvider> _tokenProvider;
        private AzureAdConfiguration _azureAdConfiguration;
        private const string Token = "testAccessToken";

        [SetUp]
        public void TestInitialize()
        {
            _azureAdConfiguration = new AzureAdConfiguration()
            {
                ClientId = "TestClientId",
                ClientSecret = "TestSecret",
                Authority = "https://Test/Authority",
                GraphApiBaseUri = "https://test.windows.net/"
            };

            _tokenProvider = new Mock<ITokenProvider>();
            _tokenProvider.Setup(t => t.GetClientAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Token);

            _graphApiSettings = new GraphApiSettings(_tokenProvider.Object, _azureAdConfiguration);
        }

        [Test]
        public async Task Should_return_access_token_string()
        {

            var accessToken = await _graphApiSettings.GetAccessToken();

            accessToken.Should().NotBeNullOrEmpty();
            accessToken.Should().Be(Token);
            _tokenProvider.Verify(t => t.GetClientAccessToken(It.IsAny<string>(), It.IsAny<string>(), _azureAdConfiguration.GraphApiBaseUri), Times.Once);
        }
    }
}
