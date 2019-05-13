using Microsoft.Extensions.Configuration;
using UserApi;
using UserApi.Common;

namespace Testing.Common
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
            
            Settings = new Settings();
            config.Bind(Settings);
        }
        
        public AzureAdConfiguration AzureAd { get; }
        
        public TestSettings TestSettings { get; }
        
        public Settings Settings { get; set; }
        

        public static readonly TestConfig Instance = new TestConfig();
    }
}