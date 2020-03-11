using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Testing.Common.Helpers;
using UserApi.Contract.Responses;

namespace UserApi.IntegrationTests.Controllers
{
    public class HealthCheckController : ControllerTestsBase
    {
        private readonly HealthCheckEndpoints _healthCheckEndpoints = new ApiUriFactory().HealthCheckEndpoints;
        private string _newUserId;

        [Test]
        public async Task should_get_ok_for_user_health_check()
        {
            var getResponse = await SendGetRequestAsync(_healthCheckEndpoints.CheckServiceHealth());
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getResponseModel =
                ApiRequestHelper.DeserialiseSnakeCaseJsonToResponse<UserApiHealthResponse>(getResponse.Content
                    .ReadAsStringAsync().Result);
            getResponseModel.AppVersion.Should().NotBeNull();
            getResponseModel.AppVersion.FileVersion.Should().NotBeNull();
            getResponseModel.AppVersion.InformationVersion.Should().NotBeNull();
        }

        [TearDown]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                    $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
                var result = client.SendAsync(httpRequestMessage).Result;
                result.IsSuccessStatusCode.Should().BeTrue($"{_newUserId} should be deleted");
                _newUserId = null;
            }
        }
    }
}