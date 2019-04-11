using System.Security.Claims;

namespace UserApi.Security.CustomToken
{
    public interface ICustomJwtTokenHandler
    {
        ClaimsPrincipal GetPrincipal(string token);
        bool IsValidToken(string token);
    }
}