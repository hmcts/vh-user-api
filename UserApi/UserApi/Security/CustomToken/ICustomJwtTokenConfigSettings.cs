namespace UserApi.Security.CustomToken
{
    public interface ICustomJwtTokenConfigSettings
    {
        int ExpiresInMinutes { get; }
        string Secret { get; }
        string Audience { get; }
    }
}