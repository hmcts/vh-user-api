using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TechTalk.SpecFlow;
using Testing.Common;
using Testing.Common.Helpers;
using UserApi.AcceptanceTests.Contexts;
using UserApi.AcceptanceTests.Helpers;
using UserApi.Common;
using UserApi.Security;

namespace UserApi.AcceptanceTests.Steps
{
    [Binding]
    public abstract class BaseSteps
    {
        protected BaseSteps()
        {
        }

        [BeforeTestRun]
        public static void OneTimeSetup(AcTestContext testContext)
        {
            var azureAdConfiguration = TestConfig.Instance.AzureAd;
            testContext.TestSettings = TestConfig.Instance.TestSettings;

            testContext.BearerToken = new TokenProvider(azureAdConfiguration).GetClientAccessToken(
                testContext.TestSettings.TestClientId, testContext.TestSettings.TestClientSecret,
                azureAdConfiguration.VhUserApiResourceId);

            testContext.GraphApiToken = new TokenProvider(azureAdConfiguration).GetClientAccessToken(
                azureAdConfiguration.ClientId, azureAdConfiguration.ClientSecret,
                "https://graph.microsoft.com");

            var apiTestsOptions = TestConfig.Instance.GetFromSection<AcceptanceTestConfiguration>("AcceptanceTestSettings");
            testContext.BaseUrl = apiTestsOptions.UserApiBaseUrl;
        }

        [BeforeTestRun]
        public static void CheckHealth(AcTestContext testContext)
        {
            var endpoint = new ApiUriFactory().HealthCheckEndpoints;
            testContext.Request = testContext.Get(endpoint.CheckServiceHealth());
            testContext.Response = testContext.Client().Execute(testContext.Request);
            testContext.Response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
