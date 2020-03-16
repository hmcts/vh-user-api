using UserApi.Common;

namespace Testing.Common.Configuration
{
    public class Config
    {
        public AzureAdConfiguration AzureAdConfiguration { get; set; }
        public TestSettings TestSettings { get; set; }
        public VhServices VhServices { get; set; }
    }
}
