using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;

namespace UserApi.UnitTests.Helpers
{
    public class GraphApiSettingsTests
    {
        private GraphApiSettings graphApiSettings;
        private Mock<ITokenProvider> tokenProvider;
        private AzureAdConfiguration azureAdConfiguration;
        private string token = "testAccessToken";
        private string graphApiBaseUriWindows => "https://graph.windows.net/";

        [SetUp]
        public void TestInitialize()
        {
            azureAdConfiguration = new AzureAdConfiguration()
            {
                ClientId = "TestClientId",
                ClientSecret = "TestSecret",
                Authority = "https://Test/Authority",
                VhUserApiResourceId = "TestResourceId",
                GraphApiBaseUri = "https://test.windows.net/"
            };

            tokenProvider = new Mock<ITokenProvider>();
            tokenProvider.Setup(t => t.GetClientAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(token);

            graphApiSettings = new GraphApiSettings(tokenProvider.Object, azureAdConfiguration);
        }

        [Test]
        public void Should_return_access_token_string()
        {

            var accessToken = graphApiSettings.AccessToken;

            accessToken.Should().NotBeNullOrEmpty();
            accessToken.Should().Be(token);
            tokenProvider.Verify(t => t.GetClientAccessToken(It.IsAny<string>(), It.IsAny<string>(), azureAdConfiguration.GraphApiBaseUri), Times.Once);
        }

        [Test]
        public void Should_return_access_token_windows_string()
        {

            var accessToken = graphApiSettings.AccessTokenWindows;

            accessToken.Should().NotBeNullOrEmpty();
            accessToken.Should().Be(token);
            tokenProvider.Verify(t => t.GetClientAccessToken(It.IsAny<string>(), It.IsAny<string>(), graphApiBaseUriWindows), Times.Once);
        }
    }
}
