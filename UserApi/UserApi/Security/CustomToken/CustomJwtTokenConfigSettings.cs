namespace UserApi.Security.CustomToken
{
    public class CustomJwtTokenConfigSettings : ICustomJwtTokenConfigSettings
    {
        public CustomJwtTokenConfigSettings(int expiresInMinutes, string secret, string audience)
        {
            ExpiresInMinutes = expiresInMinutes;
            Secret = secret;
            Audience = audience;
        }

        public int ExpiresInMinutes { get; }
        public string Secret { get; }
        public string Audience { get; }
    }
}