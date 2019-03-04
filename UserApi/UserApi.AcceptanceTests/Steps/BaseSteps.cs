using System.Net;
using FluentAssertions;
using TechTalk.SpecFlow;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public abstract class BaseSteps
    {
        protected BaseSteps()
        {
        }
      
        [BeforeTestRun]
        public static void CheckHealth(AcTestContext testContext)
        {
            //var endpoint = new ApiUriFactory().HealthCheckEndpoints;
            //testContext.Request = testContext.Get(endpoint.HealthCheck);
            //testContext.Response = testContext.Client().Execute(testContext.Request);
            //testContext.Response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
