using NUnit.Framework;
using System;
using Testing.Common.Helper;

namespace UserApi.AcceptanceTests
{
    [SetUpFixture]
    public class TestSetupFixture
    {
        [OneTimeSetUp]
        public void ZapStart()
        {
            Zap.Start();
        }

        [OneTimeTearDown]
        public void ZapReport()
        {
            Zap.ReportAndShutDown("UserApi-Acceptance");
        }
    }
}
