using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using TechTalk.SpecFlow;
using Testing.Common;
using UserApi.IntegrationTests.Contexts;
using UserApi.Security;

namespace UserApi.IntegrationTests.Hooks
{
    [Binding]
    public static class ApiHooks
    {
        [BeforeTestRun]
        public static void OneTimeSetup(TestContext testContext)
        {
            var webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(c => c.AddServerHeader = false)
                .UseEnvironment("Development")
                .UseStartup<Startup>();
            testContext.Server = new TestServer(webHostBuilder);

            GetClientAccessTokenForUserApi(testContext);
        }

        private static void GetClientAccessTokenForUserApi(TestContext testContext)
        {
            testContext.TestSettings = TestConfig.Instance.TestSettings;

            var azureAdConfig = TestConfig.Instance.AzureAd;

            testContext.BearerToken = new TokenProvider(azureAdConfig).GetClientAccessToken(
                azureAdConfig.ClientId, azureAdConfig.ClientSecret,
                azureAdConfig.VhUserApiResourceId);

            testContext.GraphApiToken = new TokenProvider(azureAdConfig).GetClientAccessToken(
                azureAdConfig.ClientId, azureAdConfig.ClientSecret,
                "https://graph.microsoft.com");
        }

        [BeforeScenario]
        public static void BeforeApiScenario(TestContext testContext)
        {
            testContext.Test.NewUserId = string.Empty;
        }

        [AfterTestRun]
        public static void OneTimeTearDown(TestContext testContext)
        {
            testContext.Server.Dispose();
        }
    }
}
