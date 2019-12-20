namespace UserApi.Security
{
    public interface ITokenProvider
    {
        string GetClientAccessToken(string clientId, string clientSecret, string[] scopes);
    }
}