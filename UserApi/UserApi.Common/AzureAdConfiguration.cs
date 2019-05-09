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
        public string VhBookingsApiResourceId { get; set; }
    }

    public class HealthConfiguration
    {
        public string HealthCheckEmail { get; set; }
    }

    public class AppConfigSettings
    {
        /// <summary>
        ///     Flag to determine the list of judges to be displayed (Live/ Live and Test).
        /// </summary>
        public bool IsLive { get; set; }
    }
}