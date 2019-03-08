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
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddUserSecrets<Startup>();

            var configRoot = configRootBuilder.Build();

            var azureAdConfigurationOptions =
                Options.Create(configRoot.GetSection("AzureAd").Get<AzureAdConfiguration>());
            var testSettingsOptions = Options.Create(configRoot.GetSection("Testing").Get<TestSettings>());

            var azureAdConfiguration = azureAdConfigurationOptions.Value;
            testContext.TestSettings = testSettingsOptions.Value;

            testContext.BearerToken = new TokenProvider(azureAdConfigurationOptions).GetClientAccessToken(
                testContext.TestSettings.TestClientId, testContext.TestSettings.TestClientSecret,
                azureAdConfiguration.VhUserApiResourceId);

            testContext.GraphApiToken = new TokenProvider(azureAdConfigurationOptions).GetClientAccessToken(
                testContext.TestSettings.TestClientId, testContext.TestSettings.TestClientSecret,
                "https://graph.microsoft.com");

            var apiTestsOptions =
                Options.Create(configRoot.GetSection("AcceptanceTestSettings").Get<AcceptanceTestConfiguration>());
            var apiTestSettings = apiTestsOptions.Value;
            testContext.BaseUrl = apiTestSettings.UserApiBaseUrl;
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
