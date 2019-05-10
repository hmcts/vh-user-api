using Microsoft.Extensions.Configuration;
using UserApi.Common;

namespace UserApi.IntegrationTests
{
    public class TestConfig
    {
        private TestConfig()
        {
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();
            
            AzureAd = new AzureAdConfiguration();
            configRootBuilder.Build().GetSection("AzureAd").Bind(AzureAd);
        }
        
        public AzureAdConfiguration AzureAd { get; }
        

        public static readonly TestConfig Instance = new TestConfig();
    }
}