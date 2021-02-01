using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTests.Common.Api;
using AcceptanceTests.Common.Configuration;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TechTalk.SpecFlow;
using Testing.Common.Configuration;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Common;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public class ConfigHooks
    {
        private readonly IConfigurationRoot _configRoot;

        public ConfigHooks(TestContext context)
        {
            _configRoot = ConfigurationManager.BuildConfig("CF55F1BB-0EE3-456A-A566-70E56AC24C95", "de27c4e5-a750-4ee5-af7e-592e4ee78ab2");
            context.Config = new Config();
            context.UserAccounts = new List<UserAccount>();
            context.Tokens = new UserApiTokens();
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
            context.Config.AzureAdConfiguration = Options.Create(_configRoot.GetSection("AzureAd").Get<AzureAdConfiguration>()).Value;
            context.Config.AzureAdConfiguration.Authority += context.Config.AzureAdConfiguration.TenantId;
            ConfigurationManager.VerifyConfigValuesSet(context.Config.AzureAdConfiguration);
        }

        private void RegisterTestUserSecrets(TestContext context)
        {
            context.Config.TestSettings = Options.Create(_configRoot.GetSection("Testing").Get<TestSettings>()).Value;
            ConfigurationManager.VerifyConfigValuesSet(context.Config.TestSettings);
            context.Config.TestUserPassword = _configRoot["TestDefaultPassword"];
            context.Config.TestUserPassword.Should().NotBeNullOrWhiteSpace();
        }

        private void RegisterTestUsers(TestContext context)
        {
            context.UserAccounts = Options.Create(_configRoot.GetSection("UserAccounts").Get<List<UserAccount>>()).Value;
            context.UserAccounts.Should().NotBeNullOrEmpty();
            foreach (var user in context.UserAccounts)
            {
                user.Key = user.Lastname;
                user.Username = $"{user.DisplayName.Replace(" ", "")}@{_configRoot["ReformEmail"]}";
            }
        }

        private static void RegisterDefaultData(TestContext context)
        {
            context.Test = new Test
            {
                NewGroupId = string.Empty,
                NewUserId = string.Empty
            };
        }

        private void RegisterHearingServices(TestContext context)
        {
            context.Config.VhServices = GetTargetTestEnvironment() == string.Empty ? Options.Create(_configRoot.GetSection("VhServices").Get<VhServices>()).Value
                : Options.Create(_configRoot.GetSection($"Testing.{GetTargetTestEnvironment()}.VhServices").Get<VhServices>()).Value;
            if (context.Config.VhServices == null && GetTargetTestEnvironment() != string.Empty) throw new TestSecretsFileMissingException(GetTargetTestEnvironment());
            ConfigurationManager.VerifyConfigValuesSet(context.Config.VhServices);
        }

        private static string GetTargetTestEnvironment()
        {
            return NUnit.Framework.TestContext.Parameters["TargetTestEnvironment"] ?? string.Empty;
        }

        private static async Task GenerateBearerTokens(TestContext context)
        {
            var azureConfig = new AzureAdConfig()
            {
                Authority = context.Config.AzureAdConfiguration.Authority,
                ClientId = context.Config.AzureAdConfiguration.ClientId,
                ClientSecret = context.Config.AzureAdConfiguration.ClientSecret,
                TenantId = context.Config.AzureAdConfiguration.TenantId
            };

            context.Tokens.UserApiBearerToken = await ConfigurationManager.GetBearerToken(
                azureConfig, context.Config.VhServices.UserApiResourceId);
            context.Tokens.UserApiBearerToken.Should().NotBeNullOrEmpty();

            Zap.SetAuthToken(context.Tokens.UserApiBearerToken);

            context.Tokens.GraphApiBearerToken = await ConfigurationManager.GetBearerToken(
                azureConfig, context.Config.AzureAdConfiguration.GraphApiBaseUri);
            context.Tokens.GraphApiBearerToken.Should().NotBeNullOrEmpty();
        }
    }

    internal class AzureAdConfig : IAzureAdConfig
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
    }
}
