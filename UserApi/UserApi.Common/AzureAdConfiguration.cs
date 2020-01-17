namespace UserApi.Common
{
    public class AzureAdConfiguration
    {
        public string Authority { get; set; }
        public string TenantId { get; set; }
        public string VhUserApiResourceId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GraphApiBaseUri { get; set; }
    }
}