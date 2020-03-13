using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTests.Common.Configuration;
using AcceptanceTests.Common.Configuration.Users;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TechTalk.SpecFlow;
using Testing.Common.Configuration;
using UserApi.AcceptanceTests.Configuration;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Common;
using AzureAdConfig = UserApi.AcceptanceTests.Configuration.AzureAdConfig;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public class ConfigHooks
    {
        private readonly IConfigurationRoot _configRoot;
        public ConfigHooks(TestContext context)
        {
            _configRoot = ConfigurationManager.BuildConfig("CF55F1BB-0EE3-456A-A566-70E56AC24C95", GetTargetEnvironment());
            context.UserAccounts = new List<UserAccount>();
            context.UserApiConfig = new UserApiConfig();
        }

        private static string GetTargetEnvironment()
        {
            return NUnit.Framework.TestContext.Parameters["TargetEnvironment"] ?? "";
        }

        [BeforeScenario(Order = (int)HooksSequence.ConfigHooks)]
        public async Task RegisterSecrets(TestContext context)
        {
            RegisterAzureSecrets(context);
            RegisterTestUserSecrets(context);
            RegisterTestUsers(context);
            RegisterDefaultData(context);
            RegisterHearingServices(context);
            await GenerateBearerTokens(context);
        }

        private void RegisterAzureSecrets(TestContext context)
        {
            context.UserApiConfig.AzureAdConfiguration = Options.Create(_configRoot.GetSection("AzureAd").Get<AzureAdConfiguration>()).Value;
            context.UserApiConfig.AzureAdConfiguration.Authority += context.UserApiConfig.AzureAdConfiguration.TenantId;
            ConfigurationManager.VerifyConfigValuesSet(context.UserApiConfig.AzureAdConfiguration);
        }

        private void RegisterTestUserSecrets(TestContext context)
        {
            context.UserApiConfig.TestConfig = Options.Create(_configRoot.GetSection("Testing").Get<TestSettings>()).Value;
            context.UserApiConfig.TestConfig.ReformEmail = _configRoot["ReformEmail"];
            ConfigurationManager.VerifyConfigValuesSet(context.UserApiConfig.TestConfig);
        }

        private void RegisterTestUsers(TestContext context)
        {
            context.UserAccounts = Options.Create(_configRoot.GetSection("UserAccounts").Get<List<UserAccount>>()).Value;
            context.UserAccounts.Should().NotBeNullOrEmpty();
            foreach (var user in context.UserAccounts)
            {
                user.Key = user.Lastname;
                user.Username = $"{user.DisplayName.Replace(" ", "")}@{context.UserApiConfig.TestConfig.ReformEmail}";
            }
        }

        private static void RegisterDefaultData(TestContext context)
        {
            context.Test = new Test();
        }

        private void RegisterHearingServices(TestContext context)
        {
            context.UserApiConfig.VhServices = Options.Create(_configRoot.GetSection("VhServices").Get<VhServicesConfig>()).Value;
            ConfigurationManager.VerifyConfigValuesSet(context.UserApiConfig.VhServices);
        }

        private static async Task GenerateBearerTokens(TestContext context)
        {
            var azureConfig = new AzureAdConfig()
            {
                Authority = context.UserApiConfig.AzureAdConfiguration.Authority,
                ClientId = context.UserApiConfig.AzureAdConfiguration.ClientId,
                ClientSecret = context.UserApiConfig.AzureAdConfiguration.ClientSecret,
                TenantId = context.UserApiConfig.AzureAdConfiguration.TenantId
            };

            context.BearerToken = await ConfigurationManager.GetBearerToken(
                azureConfig, context.UserApiConfig.VhServices.UserApiResourceId);
            context.BearerToken.Should().NotBeNullOrEmpty();

            context.GraphApiToken = await ConfigurationManager.GetBearerToken(
                azureConfig, context.UserApiConfig.AzureAdConfiguration.GraphApiUri);
            context.GraphApiToken.Should().NotBeNullOrEmpty();
        }
    }
}
