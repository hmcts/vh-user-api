using Microsoft.Extensions.Configuration;
using Testing.Common;
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

            var config = configRootBuilder.Build();
            
            AzureAd = new AzureAdConfiguration();
            config.GetSection("AzureAd").Bind(AzureAd);

            TestSettings = new TestSettings();
            config.GetSection("Testing").Bind(TestSettings);
        }
        
        public AzureAdConfiguration AzureAd { get; }
        
        public TestSettings TestSettings { get; }
        

        public static readonly TestConfig Instance = new TestConfig();
    }
}