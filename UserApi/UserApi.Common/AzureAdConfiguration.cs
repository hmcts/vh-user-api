namespace UserApi.Common
{
    public class AzureAdConfiguration
    {
        public string Authority { get; set; }
        public string TenantId { get; set; }
        public string AppIdUri { get; set; }
        public string Scope { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GraphApiBaseUri { get; set; }
        public AzureAdGraphApiConfig AzureAdGraphApiConfig { get; set; }
    }

    public class AzureAdGraphApiConfig
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}