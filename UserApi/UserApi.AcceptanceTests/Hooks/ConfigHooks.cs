using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Testing.Common.Configuration;
using UserApi.AcceptanceTests.Contexts;
using UserApi.Common;
using UserApi.Common.Security;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public class ConfigHooks
    {
        private readonly IConfigurationRoot _configRoot;

        public ConfigHooks(TestContext context)
        {
            _configRoot = ConfigRootBuilder.Build(inlcudeTestUserSecrets:true);
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
        }

        private void RegisterTestUserSecrets(TestContext context)
        {
            context.Config.TestSettings = Options.Create(_configRoot.GetSection("Testing").Get<TestSettings>()).Value;
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
            if (context.Config.VhServices == null && GetTargetTestEnvironment() != string.Empty)
            {
                throw new InvalidOperationException(
                    $"Missing test secrets for running against: {GetTargetTestEnvironment()}");
            }
        }

        private static string GetTargetTestEnvironment()
        {
            return NUnit.Framework.TestContext.Parameters["TargetTestEnvironment"] ?? string.Empty;
        }

        private static async Task GenerateBearerTokens(TestContext context)
        {
            var adConfig = context.Config.AzureAdConfiguration;

            context.Tokens.UserApiBearerToken = await new TokenProvider(
                adConfig).GetClientAccessToken(adConfig.ClientId, adConfig.ClientSecret,
                context.Config.VhServices.UserApiResourceId);
            context.Tokens.UserApiBearerToken.Should().NotBeNullOrEmpty();

            context.Tokens.GraphApiBearerToken = await new TokenProvider(adConfig).GetClientAccessToken(
                adConfig.ClientId, adConfig.ClientSecret, context.Config.AzureAdConfiguration.GraphApiBaseUri);
            context.Tokens.GraphApiBearerToken.Should().NotBeNullOrEmpty();
        }
    }
}
