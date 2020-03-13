using AcceptanceTests.Common.Api.Users;
using AcceptanceTests.Common.Configuration;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public class RegisterApisHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.RegisterApisHooks)]
        public void RegisterApis(TestContext context)
        {
            context.Api = new UserApiManager(context.UserApiConfig.VhServices.UserApiUrl, context.BearerToken);
            ConfigurationManager.VerifyConfigValuesSet(context.Api);
        }
    }
}
