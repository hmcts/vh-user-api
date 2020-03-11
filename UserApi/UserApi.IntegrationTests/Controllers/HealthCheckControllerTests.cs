using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static Testing.Common.Helpers.UserApiUriFactory.HealthCheckEndpoints;


namespace UserApi.IntegrationTests.Controllers
{
    public class HealthCheckController : ControllerTestsBase
    {
        private string _newUserId;

        [Test]
        public async Task Should_get_ok_for_user_health_check()
        {
            var getResponse = await SendGetRequestAsync(CheckServiceHealth());
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TearDown]
        public void ClearUp()
        {
            if (string.IsNullOrWhiteSpace(_newUserId)) return;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GraphApiToken);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
                $@"https://graph.microsoft.com/v1.0/users/{_newUserId}");
            var result = client.SendAsync(httpRequestMessage).Result;
            result.IsSuccessStatusCode.Should().BeTrue($"{_newUserId} should be deleted");
            _newUserId = null;
        }
    }
}