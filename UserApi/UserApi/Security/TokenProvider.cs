using Microsoft.Identity.Client;
using System;
using UserApi.Common;

namespace UserApi.Security
{
    public interface ITokenProvider
    {
        string GetClientAccessToken(string clientId, string clientSecret, string[] scopes);
        AuthenticationResult GetAuthorisationResult(string clientId, string clientSecret, string[] scopes);
    }

    public class TokenProvider : ITokenProvider
    {
        private readonly AzureAdConfiguration _azureAdConfiguration;

        public TokenProvider(AzureAdConfiguration azureAdConfiguration)
        {
            _azureAdConfiguration = azureAdConfiguration;
        }

        public string GetClientAccessToken(string clientId, string clientSecret, string[] scopes)
        {
            var result = GetAuthorisationResult(clientId, clientSecret, scopes);
            return result.AccessToken;
        }

        public AuthenticationResult GetAuthorisationResult(string clientId, string clientSecret, string[] scopes)
        {
            AuthenticationResult result;

            var app = ConfidentialClientApplicationBuilder.Create(clientId)
           .WithAuthority(AzureCloudInstance.AzurePublic, _azureAdConfiguration.TenantId)
           .WithClientSecret(clientSecret)
           .Build();

            try
            {
                result = app.AcquireTokenForClient(scopes)
                    .ExecuteAsync().Result;
            }
            catch (MsalServiceException ex)
            {
                throw new UnauthorizedAccessException();
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException();
            }

            return result;
        }
    }
}