using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace UserApi.Security.CustomToken
{
    public class CustomJwtTokenProvider : ICustomJwtTokenProvider
    {
        private readonly ICustomJwtTokenConfigSettings _customJwtTokenConfigSettings;

        public CustomJwtTokenProvider(ICustomJwtTokenConfigSettings customJwtTokenConfigSettings)
        {
            _customJwtTokenConfigSettings = customJwtTokenConfigSettings;
        }

        public string GenerateToken(string basedOn, int expiresInMinutes)
        {
            byte[] key = Convert.FromBase64String(_customJwtTokenConfigSettings.Secret);
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(key);
            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, basedOn) }),
                Audience = _customJwtTokenConfigSettings.Audience,
                Issuer = _customJwtTokenConfigSettings.Issuer,
                Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token = handler.CreateJwtSecurityToken(descriptor);
            return handler.WriteToken(token);
        }

    }
}