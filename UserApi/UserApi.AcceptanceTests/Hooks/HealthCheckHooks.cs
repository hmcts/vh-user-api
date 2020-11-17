using System.Net;
using FluentAssertions;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Contexts;
using static Testing.Common.Helpers.UserApiUriFactory.HealthCheckEndpoints;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public static class HealthCheckHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.HealthCheckHooks)]
        public static void CheckApiHealth(TestContext context)
        {
            var request = context.Get(CheckServiceHealth);
            var response = context.Client().Execute(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.IsSuccessful.Should().BeTrue();
        }
    }
}
