using System.Net;
using System.Net.Http;
using Moq;
using NUnit.Framework;
using UserApi.Helper;
using UserApi.Services;

namespace UserApi.UnitTests.Services
{
    public class GraphApiClientTests
    {
        private Mock<IGraphApiSettings> _graphApiSettings;
        private Mock<ISecureHttpRequest> _secureHttpRequest;
        private GraphApiClient _client;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<ISecureHttpRequest>();
            _graphApiSettings = new Mock<IGraphApiSettings>();
            _client = new GraphApiClient(_secureHttpRequest.Object, _graphApiSettings.Object, new Settings());
        }
        
        /// <summary>
        /// Since the <see cref="ISecureHttpRequest"/> does not capture invalid response codes we need to raise this.
        /// Going forwards this test ought to be removed as the API should raise generic http response exceptions on failure.
        /// </summary>
        /// <returns></returns>
        [Test]
        public void should_raise_exception_on_unsuccessful_response()
        {
            var invalidResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("test")
            };
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(invalidResponse);

            Assert.ThrowsAsync<IdentityServiceApiException>(() => _client.GetUsernamesStartingWithAsync("bob"));
        }
    }
}