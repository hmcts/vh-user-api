namespace UserApi.Security
{
    public interface ITokenProvider
    {
        string GetClientAccessToken(string tenantId, string clientId, string clientSecret, string[] scopes);
    }
}