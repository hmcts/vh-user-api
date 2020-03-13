using AcceptanceTests.Common.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using TechTalk.SpecFlow;
using Testing.Common.Configuration;
using UserApi.Common;
using UserApi.IntegrationTests.Configuration;
using UserApi.IntegrationTests.Contexts;
using UserApi.Security;

namespace UserApi.IntegrationTests.Hooks
{
    [Binding]
    public static class ConfigHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.ConfigHooks)]
        public static void RegisterSecrets(TestContext context)
        {
            RegisterAzureSecrets(context);
            RegisterTestUserSecrets(context);
            RegisterDefaultData(context);
            RegisterHearingServices(context);
            RegisterServer(context);
            GenerateBearerTokens(context);
        }

        private static void RegisterAzureSecrets(TestContext context)
        {
            context.UserApiConfig = new UserApiConfig { AzureAdConfiguration = new AzureAdConfiguration() };
            context.UserApiConfig.AzureAdConfiguration = TestConfig.Instance.AzureAd;
            ConfigurationManager.VerifyConfigValuesSet(context.UserApiConfig.AzureAdConfiguration);
        }

        private static void RegisterTestUserSecrets(TestContext context)
        {
            context.UserApiConfig.TestSettings = TestConfig.Instance.TestSettings;
            context.UserApiConfig.TestSettings.ReformEmail = TestConfig.Instance.Settings.ReformEmail;
            ConfigurationManager.VerifyConfigValuesSet(context.UserApiConfig.TestSettings);
        }

        private static void RegisterDefaultData(TestContext context)
        {
            context.Test = new Test();
        }

        private static void RegisterHearingServices(TestContext context)
        {
            context.UserApiConfig.VhServices = TestConfig.Instance.VhServices;
            ConfigurationManager.VerifyConfigValuesSet(context.UserApiConfig.VhServices);
        }

        private static void RegisterServer(TestContext context)
        {
            var webHostBuilder = WebHost.CreateDefaultBuilder()
                .UseKestrel(c => c.AddServerHeader = false)
                .UseEnvironment("Development")
                .UseStartup<Startup>();
            context.Server = new TestServer(webHostBuilder);
        }

        private static void GenerateBearerTokens(TestContext testContext)
        {
            testContext.BearerToken = new TokenProvider(testContext.UserApiConfig.AzureAdConfiguration).GetClientAccessToken(
                testContext.UserApiConfig.AzureAdConfiguration.ClientId, testContext.UserApiConfig.AzureAdConfiguration.ClientSecret,
                testContext.UserApiConfig.VhServices.UserApiResourceId);

            testContext.GraphApiToken = new TokenProvider(testContext.UserApiConfig.AzureAdConfiguration).GetClientAccessToken(
                testContext.UserApiConfig.AzureAdConfiguration.ClientId, testContext.UserApiConfig.AzureAdConfiguration.ClientSecret,
                testContext.UserApiConfig.AzureAdConfiguration.GraphApiUri);
        }

        [AfterTestRun]
        public static void OneTimeTearDown(TestContext testContext)
        {
            testContext.Server.Dispose();
        }
    }
}
