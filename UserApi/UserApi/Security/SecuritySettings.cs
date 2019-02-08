namespace UserApi.Security
{
    public class SecuritySettings
    {
        public string GraphApiBaseUri { get; set; }
        public string AppInsightsKey { get; set; }
        public string TenantId { get; set; }
        public string Authority { get; set; }
        public string ClientSecret { get; set; }
        public string ClientId { get; set; }
        public KeyVaultSettings KeyVaultSettings { get; set; } = new KeyVaultSettings();
        public string HearingsApiResourceId { get; set; }
        public string BookHearingApiResourceId { get; set; }
        public string BookHearingUIClientId { get; set; }
        public string BookHearingUIClientSecret { get; set; }
    }
}