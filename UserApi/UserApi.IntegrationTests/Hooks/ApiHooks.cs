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

            apiTestContext.UserApiToken = tokenProvider.GetClientAccessToken
            (
                TestConfig.Instance.AzureAd.TenantId,
                TestConfig.Instance.AzureAd.ClientId,
                TestConfig.Instance.AzureAd.ClientSecret,
                new []{ $"{TestConfig.Instance.AzureAd.Scope}/.default"}
            );

            apiTestContext.GraphApiToken = tokenProvider.GetClientAccessToken
            (
                TestConfig.Instance.AzureAd.AzureAdGraphApiConfig.TenantId,
                TestConfig.Instance.AzureAd.AzureAdGraphApiConfig.ClientId,
                TestConfig.Instance.AzureAd.AzureAdGraphApiConfig.ClientSecret,
                new []{ $"{TestConfig.Instance.AzureAd.GraphApiBaseUri}.default"}
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
