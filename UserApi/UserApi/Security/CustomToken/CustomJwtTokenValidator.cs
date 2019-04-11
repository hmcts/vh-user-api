using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace UserApi.Security.CustomToken
{
    public class CustomJwtTokenHandler : ICustomJwtTokenHandler
    {
        private readonly ICustomJwtTokenConfigSettings _customJwtTokenConfigSettings;

        public CustomJwtTokenHandler(ICustomJwtTokenConfigSettings customJwtTokenConfigSettings)
        {
            _customJwtTokenConfigSettings = customJwtTokenConfigSettings;
        }

        public ClaimsPrincipal GetPrincipal(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = (JwtSecurityToken)tokenHandler.ReadToken(token);
            if (jwtToken == null)
            {
                return null;
            }

            byte[] key = Convert.FromBase64String(_customJwtTokenConfigSettings.Secret);
            var parameters = new TokenValidationParameters()
            {
                RequireExpirationTime = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            var principal = tokenHandler.ValidateToken(token, parameters, out var securityToken);
            return principal;
        }

        public bool IsValidToken(string token)
        {
            var principal = GetPrincipal(token);

            if (principal?.Identity != null && principal.Identity is ClaimsIdentity claimsIdentity)
            {
                var identity = claimsIdentity;

                return identity.FindFirst(ClaimTypes.Name) != null;
            }

            return false;
        }
    }
}