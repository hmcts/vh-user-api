using Microsoft.Extensions.Configuration;
using System.Net;
using Testing.Common.Configuration;

namespace Testing.Common.Helper
{
    public static class Zap
    {    
        private static ZapConfiguration ZapConfiguration =>
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("CF55F1BB-0EE3-456A-A566-70E56AC24C95")
                .Build()
                .GetSection("ZapConfiguration")
                .Get<ZapConfiguration>();

        public static IWebProxy WebProxy => ZapConfiguration.SetupProxy ? new WebProxy($"http://{ZapConfiguration.ApiAddress}:{ZapConfiguration.ApiPort}", false) : null;

        public static bool SetupProxy => ZapConfiguration.SetupProxy;
    }
}
