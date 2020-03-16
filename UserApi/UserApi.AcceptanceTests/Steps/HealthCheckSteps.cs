using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Contexts;
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
            _context.Request = _context.Get(CheckServiceHealth);       
        }
    }
}
