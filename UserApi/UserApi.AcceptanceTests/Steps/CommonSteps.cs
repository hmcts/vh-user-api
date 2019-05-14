using System.Net;
using FluentAssertions;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class CommonSteps : BaseSteps
    {
        private readonly ScenarioContext _context;
        private readonly AcTestContext _acTestContext;

        public CommonSteps(ScenarioContext injectedContext, AcTestContext acTestContext)
        {
            _context = injectedContext;
            _acTestContext = acTestContext;
        }

        [When(@"I send the request to the endpoint")]
        public void WhenISendTheRequestToTheEndpoint()
        {
            _acTestContext.Response = _acTestContext.Client().Execute(_acTestContext.Request);
            if (_acTestContext.Response.Content != null)
                _acTestContext.Json = _acTestContext.Response.Content;
        }

        [Then(@"the response should have the status (.*) and success status (.*)")]
        public void ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode httpStatusCode, bool isSuccess)
        {
            _acTestContext.Response.StatusCode.Should().Be(httpStatusCode, $"When {_acTestContext.Request.Method}ing to {_acTestContext.Request.Resource}");
            _acTestContext.Response.IsSuccessful.Should().Be(isSuccess);
        }
    }
}
