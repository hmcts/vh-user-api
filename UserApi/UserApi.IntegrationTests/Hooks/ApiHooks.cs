using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TechTalk.SpecFlow;
using Testing.Common;
using UserApi.Common;
using UserApi.IntegrationTests.Contexts;
using UserApi.Security;

namespace UserApi.IntegrationTests.Hooks
{
    [Binding]
    public sealed class ApiHooks
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
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();

            var configRoot = configRootBuilder.Build();

            var testSettingsOptions = Options.Create(configRoot.GetSection("Testing").Get<TestSettings>());
            apiTestContext.TestSettings = testSettingsOptions.Value;

            var azureAdConfigOptions = Options.Create(configRoot.GetSection("AzureAd").Get<AzureAdConfiguration>());
            var azureAdConfiguration = azureAdConfigOptions.Value;

            apiTestContext.BearerToken = new TokenProvider(azureAdConfigOptions).GetClientAccessToken(
                apiTestContext.TestSettings.TestClientId, apiTestContext.TestSettings.TestClientSecret,
                azureAdConfiguration.VhUserApiResourceId);

            apiTestContext.GraphApiToken = new TokenProvider(azureAdConfigOptions).GetClientAccessToken(
                apiTestContext.TestSettings.TestClientId, apiTestContext.TestSettings.TestClientSecret,
                "https://graph.microsoft.com");
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
