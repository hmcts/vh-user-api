using Microsoft.Identity.Client;
using System;

namespace UserApi.Security
{
    public class TokenProvider : ITokenProvider
    {
        public string GetClientAccessToken(string tenantId, string clientId, string clientSecret, string[] scopes)
        {
            var app = 
                ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .WithClientSecret(clientSecret)
                    .Build();

            try
            {
                var result = app.AcquireTokenForClient(scopes).ExecuteAsync().Result;
                
                return result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                throw new UnauthorizedAccessException("Exception occured during AcquireTokenForClient.", ex);
            }
        }
    }
}