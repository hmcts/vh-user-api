using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class HealthCheckSteps : BaseSteps
    {
        private readonly TestContext _context;
        private readonly HealthCheckEndpoints _endpoints = new ApiUriFactory().HealthCheckEndpoints;

        public HealthCheckSteps(TestContext context)
        {
            _context = context;
        }

        [Given(@"I have a get health request")]
        public void GivenIHaveAGetHealthRequest()
        {
            _context.Request = _context.Get(_endpoints.CheckServiceHealth());       
        }
    }
}
