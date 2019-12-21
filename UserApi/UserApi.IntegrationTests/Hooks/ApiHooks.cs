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
            var tokenProvider = new TokenProvider(azureAdConfig);

            apiTestContext.BearerToken = tokenProvider.GetClientAccessToken
            (
                apiTestContext.TestSettings.TestClientId, 
                apiTestContext.TestSettings.TestClientSecret,
                azureAdConfig.VhUserApiResourceId
            );

            apiTestContext.GraphApiToken = tokenProvider.GetClientAccessToken
            (
                azureAdConfig.ClientId, 
                azureAdConfig.ClientSecret,
                "https://graph.microsoft.com"
            );
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
