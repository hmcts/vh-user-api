using System;
using System.Security.Cryptography;
using System.Text;
using UserApi.Security.CustomToken;

namespace UserApi.Security
{
    public class MessageEncryptor
    {
        private readonly ICustomJwtTokenConfigSettings _customJwtTokenConfigSettings;

        public MessageEncryptor(ICustomJwtTokenConfigSettings customJwtTokenConfigSettings)
        {
            _customJwtTokenConfigSettings = customJwtTokenConfigSettings;
        }

        public string HashRequestTarget(string requestTarget)
        {
            var key = Convert.FromBase64String(_customJwtTokenConfigSettings.Secret);
            var requestUri = System.Web.HttpUtility.UrlEncode(requestTarget);

            var request = Encoding.UTF8.GetBytes(requestUri);
            using (var hmac = new HMACSHA256(key))
            {
                var computeHash = hmac.ComputeHash(request);
                return Convert.ToBase64String(computeHash);
            }
        }
    }
}