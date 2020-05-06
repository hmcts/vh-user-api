using TechTalk.SpecFlow;
using UserApi.IntegrationTests.Contexts;

namespace UserApi.IntegrationTests.Hooks
{
    [Binding]
    public static class ServerHooks
    {
        [AfterTestRun]
        public static void TearDownServer(TestContext testContext)
        {
            testContext.Server?.Dispose();
        }
    }
}
