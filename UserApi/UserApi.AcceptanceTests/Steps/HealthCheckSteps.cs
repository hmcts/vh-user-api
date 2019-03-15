using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public sealed class HealthCheckSteps : BaseSteps
    {
        private readonly ScenarioContext _context;
        private readonly AcTestContext _acTestContext;
        private readonly HealthCheckEndpoints _endpoints = new ApiUriFactory().HealthCheckEndpoints;

        public HealthCheckSteps(ScenarioContext injectedContext, AcTestContext acTestContext)
        {
            _context = injectedContext;
            _acTestContext = acTestContext;
        }

        [Given(@"I have a get health request")]
        public void GivenIHaveAGetHealthRequest()
        {
            _acTestContext.Request = _acTestContext.Get(_endpoints.CheckServiceHealth());       
        }
    }
}
