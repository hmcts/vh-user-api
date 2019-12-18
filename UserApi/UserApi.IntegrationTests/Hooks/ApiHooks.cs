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
        public static void OneTimeSetup(ApiTestContext apiTestContext)
        {
            var webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(c => c.AddServerHeader = false)
                .UseEnvironment("Development")
                .UseStartup<Startup>();
            apiTestContext.Server = new TestServer(webHostBuilder);

            GetClientAccessTokenForUserApi(apiTestContext);
        }

        private static void GetClientAccessTokenForUserApi(ApiTestContext apiTestContext)
        {
            apiTestContext.TestSettings = TestConfig.Instance.TestSettings;

            var azureAdConfig = TestConfig.Instance.AzureAd;

            apiTestContext.BearerToken = new TokenProvider(azureAdConfig).GetClientAccessToken(
                apiTestContext.TestSettings.TestClientId, apiTestContext.TestSettings.TestClientSecret,
                new string[] { $"{azureAdConfig.AppIdUri}/.default" });

            apiTestContext.GraphApiToken = new TokenProvider(azureAdConfig).GetClientAccessToken(
                azureAdConfig.ClientId, azureAdConfig.ClientSecret,
                new string[] { "https://graph.microsoft.com/.default" });
        }

        [BeforeScenario]
        public static void BeforeApiScenario(ApiTestContext apiTestContext)
        {
            apiTestContext.NewUserId = string.Empty;
        }

        [AfterTestRun]
        public static void OneTimeTearDown(ApiTestContext apiTestContext)
        {
            apiTestContext.Server.Dispose();
        }
    }
}
