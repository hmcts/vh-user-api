namespace UserApi.Common
{
    public class AzureAdConfiguration
    {
        public string Authority { get; set; }
        public string TenantId { get; set; }
        public string AppIdUri { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GraphApiBaseUri { get; set; }
        public string VhBookingsApiResourceId { get; set; }
    }

    public class HealthConfiguration
    {
        public string HealthCheckEmail { get; set; }
    }
}