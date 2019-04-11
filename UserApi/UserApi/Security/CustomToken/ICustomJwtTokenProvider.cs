namespace UserApi.Security.CustomToken
{
    public interface ICustomJwtTokenProvider
    {
        string GenerateToken(string basedOn, int expiresInMinutes);
    }
}