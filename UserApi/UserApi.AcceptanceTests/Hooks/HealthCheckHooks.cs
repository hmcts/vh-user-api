using AcceptanceTests.Common.Api.Healthchecks;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Contexts;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public static class HealthCheckHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.HealthCheckHooks)]
        public static void CheckApiHealth(TestContext context)
        {
            CheckUserApiHealth(context.Config.VhServices.UserApiUrl, context.Tokens.UserApiBearerToken);
        }
        private static void CheckUserApiHealth(string apiUrl, string bearerToken)
        {
            HealthcheckManager.CheckHealthOfUserApi(apiUrl, bearerToken);
        }
    }
}
