using AcceptanceTests.Common.Api.Requests;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Helpers;
using static Testing.Common.Helpers.UserApiUriFactory.HealthCheckEndpoints;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class HealthCheckSteps
    {
        private readonly TestContext _context;

        public HealthCheckSteps(TestContext context)
        {
            _context = context;
        }

        [Given(@"I have a get health request")]
        public void GivenIHaveAGetHealthRequest()
        {
            _context.Request = RequestBuilder.Get(CheckServiceHealth);       
        }
    }
}
