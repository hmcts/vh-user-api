using System.Net;
using AcceptanceTests.Common.Api.Clients;
using AcceptanceTests.Common.Api.Requests;
using FluentAssertions;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Helpers;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class CommonSteps
    {
        private readonly TestContext _c;

        public CommonSteps(TestContext context)
        {
            _c = context;
        }

        [When(@"I send the request to the endpoint")]
        public void WhenISendTheRequestToTheEndpoint()
        {
            var client = ApiClient.SetClient(_c.UserApiConfig.VhServices.UserApiUrl, _c.BearerToken);
            _c.Response = RequestExecutor.SendToApi(_c.Request, client);
        }

        [Then(@"the response should have the status (.*) and success status (.*)")]
        public void ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode httpStatusCode, bool isSuccess)
        {
            _c.Response.StatusCode.Should().Be(httpStatusCode, $"When {_c.Request.Method}ing to {_c.Request.Resource}");
            _c.Response.IsSuccessful.Should().Be(isSuccess);
        }
    }
}
