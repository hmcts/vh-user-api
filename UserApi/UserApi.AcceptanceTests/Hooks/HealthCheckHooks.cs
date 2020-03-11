using AcceptanceTests.Common.Api.Healthchecks;
using TechTalk.SpecFlow;
using UserApi.AcceptanceTests.Helpers;

namespace UserApi.AcceptanceTests.Hooks
{
    [Binding]
    public class HealthcheckHooks
    {
        [BeforeScenario(Order = (int)HooksSequence.HealthcheckHooks)]
        public void CheckApiHealth(TestContext context)
        {
            CheckUserApiHealth(context.UserApiConfig.VhServices.UserApiUrl, context.BearerToken);
        }

        private static void CheckUserApiHealth(string apiUrl, string bearerToken)
        {
            HealthcheckManager.CheckHealthOfUserApi(apiUrl, bearerToken);
        }
    }
}
