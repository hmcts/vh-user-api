using Microsoft.Extensions.Configuration;
using UserApi;
using UserApi.Common;

namespace Testing.Common
{
    public class TestConfig
    {        
        private readonly IConfigurationRoot _configuration;

        private TestConfig()
        {
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();

            _configuration = configRootBuilder.Build();

            AzureAd = new AzureAdConfiguration();
            _configuration.GetSection("AzureAd").Bind(AzureAd);

            TestSettings = new TestSettings();
            _configuration.GetSection("Testing").Bind(TestSettings);
            
            Settings = new Settings();
            _configuration.Bind(Settings);
        }
        
        public AzureAdConfiguration AzureAd { get; }
        
        public TestSettings TestSettings { get; }
        
        public Settings Settings { get; set; }

        public TType GetFromSection<TType>(string sectionName)
        {
            return _configuration.GetSection(sectionName).Get<TType>();
        }
        
        public static readonly TestConfig Instance = new TestConfig();
    }
}