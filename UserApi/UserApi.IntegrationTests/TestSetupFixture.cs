using AcceptanceTests.Common.Api;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using UserApi.Common;

namespace UserApi.IntegrationTests
{
    [SetUpFixture]
    public class TestSetupFixture
    {
        private static VhServices VhServices => new ConfigurationBuilder()
                                                            .AddJsonFile("appsettings.json")
                                                            .Build()
                                                            .GetSection("VhServices")
                                                            .Get<VhServices>();

        [OneTimeSetUp]
        public void ZapStart()
        {
            Zap.Start();
        }

        [OneTimeTearDown]
        public void ZapReport()
        {
            Zap.ReportAndShutDown("UserApi-Integration",VhServices.UserApiUrl);
        }
    }
}
