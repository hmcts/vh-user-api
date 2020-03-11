using AcceptanceTests.Common.Configuration;

namespace UserApi.AcceptanceTests.Configuration
{
    public class UserApiSecurityConfiguration : IAzureAdConfig
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
    }
}
