using System.Net;
using FluentAssertions;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class CommonSteps : BaseSteps
    {
        private readonly TestContext _context;

        public CommonSteps(TestContext context)
        {
            _context = context;
        }

        [When(@"I send the request to the endpoint")]
        public void WhenISendTheRequestToTheEndpoint()
        {
            _context.Response = _context.Client().Execute(_context.Request);
            if (_context.Response.Content != null)
                _context.Json = _context.Response.Content;
        }

        [Then(@"the response should have the status (.*) and success status (.*)")]
        public void ThenTheResponseShouldHaveTheStatusAndSuccessStatus(HttpStatusCode httpStatusCode, bool isSuccess)
        {
            _context.Response.StatusCode.Should().Be(httpStatusCode, $"When {_context.Request.Method}ing to {_context.Request.Resource}");
            _context.Response.IsSuccessful.Should().Be(isSuccess);
        }
    }
}
