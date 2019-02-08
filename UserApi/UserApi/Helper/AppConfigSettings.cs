namespace UserApi.Helper
{
    public class AppConfigSettings
    {
        public string VhApiBaseUrl { get; set; }
        public int TokenCacheExpiresIn { get; set; }
        public int APIFailureRetryTimeoutSeconds { get; set; }
    }
}