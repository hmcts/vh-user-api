using Testing.Common.Configuration;
using UserApi.Common;

namespace UserApi.IntegrationTests.Configuration
{
    public class UserApiConfig
    {
        public AzureAdConfiguration AzureAdConfiguration { get; set; }
        public TestSettings TestSettings { get; set; }
        public VhServicesConfig VhServices { get; set; }
    }
}
