using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Helper;
using UserApi.Services;
using UserApi.Services.Models;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace UserApi.UnitTests.Services
{
    public class GraphApiClientTests
    {
        private Mock<IGraphApiSettings> _graphApiSettings;
        private Mock<ISecureHttpRequest> _secureHttpRequest;
        private GraphApiClient _client;
        private string _baseUrl;
        private string _defaultPassword;
        private string userName => "bob";

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<ISecureHttpRequest>();
            _graphApiSettings = new Mock<IGraphApiSettings>();
            var settings = new Settings() { DefaultPassword = "TestPwd" };
            _defaultPassword = settings.DefaultPassword;
            _baseUrl = $"{_graphApiSettings.Object.GraphApiBaseUri}/v1.0/{_graphApiSettings.Object.TenantId}";
            _client = new GraphApiClient(_secureHttpRequest.Object, _graphApiSettings.Object, settings);
        }

        [Test]
        public async Task Should_return_user_names_starting_with_text()
        {
            var text = "test'user";
            var filter = $"startswith(userPrincipalName,'{text.Replace("'", "''")}')";
            var queryUrl = $"{_baseUrl}/users?$filter={filter}";

            var user = new Microsoft.Graph.User() { UserPrincipalName = "TestUser" };
            var azureAdGraphQueryResponse = new AzureAdGraphQueryResponse<Microsoft.Graph.User>() { Value = new List<Microsoft.Graph.User> { user } };
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(azureAdGraphQueryResponse, HttpStatusCode.OK));

            var response = await _client.GetUsernamesStartingWithAsync(text);

            response.Should().NotBeNull();
            var users = response.ToList();
            users.Count.Should().Be(1);
            users.First().Should().Be("TestUser");
            _secureHttpRequest.Verify(x => x.GetAsync(_graphApiSettings.Object.AccessToken, queryUrl), Times.Once);
        }
     

        /// <summary>
        /// Since the <see cref="ISecureHttpRequest"/> does not capture invalid response codes we need to raise this.
        /// Going forwards this test ought to be removed as the API should raise generic http response exceptions on failure.
        /// </summary>
        /// <returns></returns>
        [Test]
        public void should_raise_exception_on_unsuccessful_response()
        {
            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("test", HttpStatusCode.Unauthorized));

            Assert.ThrowsAsync<IdentityServiceApiException>(() => _client.GetUsernamesStartingWithAsync("bob"));
        }
        
        [Test]
        public async Task should_raise_IdentityServiceApiException_on_unsuccessful_response_on_delete()
        {            
            _secureHttpRequest.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("test", HttpStatusCode.Unauthorized));

           var exception = Assert.ThrowsAsync<IdentityServiceApiException>(async () => await _client.DeleteUserAsync("bob"));

            exception.Should().NotBeNull();
            exception.Message.Should().Be("Failed to call API: Unauthorized\r\ntest");
        }
        
        [Test]
        public void should_be_successful_response_on_delete()
        {
            var queryUrl = $"{_baseUrl}/users/{userName}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
            
            _secureHttpRequest.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            Assert.DoesNotThrowAsync(() => _client.DeleteUserAsync(userName));
            _secureHttpRequest.Verify(x => x.DeleteAsync(_graphApiSettings.Object.AccessToken, queryUrl), Times.Once);
        }

        [Test]
        public void should_be_successful_response_on_update()
        {
            var user = new
            {
                userPrincipalName = userName,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = true,
                    password = _defaultPassword
                }
            };

            var queryUrl = $"{_baseUrl}/users/{userName}";
            var json = JsonConvert.SerializeObject(user);
            var stringContent = new StringContent(json);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            _secureHttpRequest.Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(responseMessage);

            Assert.DoesNotThrowAsync(() => _client.UpdateUserAsync(userName));

            _secureHttpRequest.Verify(x => x.PatchAsync(_graphApiSettings.Object.AccessToken, It.IsAny<StringContent>(), queryUrl), Times.Once);
        }

        [Test]
        public void should_raise_IdentityServiceApiException_on_unsuccessful_response_on_update()
        {
            _secureHttpRequest.Setup(x => x.PatchAsync(It.IsAny<string>(), It.IsAny<StringContent>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("test",HttpStatusCode.BadRequest));

            Assert.ThrowsAsync<IdentityServiceApiException>(() => _client.UpdateUserAsync(userName));
        }
    }
}