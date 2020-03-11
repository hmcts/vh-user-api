namespace UserApi.AcceptanceTests.Configuration
{
    public class UserApiConfig
    {
        public UserApiSecurityConfiguration AzureAdConfiguration { get; set; }
        public UserApiTestConfig TestConfig { get; set; }
        public UserApiVhServicesConfig VhServices { get; set; }
    }
}
