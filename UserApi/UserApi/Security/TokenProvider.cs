using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using UserApi.Common;

namespace UserApi.Security
{
    public interface ITokenProvider
    {
        string GetClientAccessToken(string clientId, string clientSecret, string clientResource);
    }

    public class TokenProvider : ITokenProvider
    {
        private readonly AzureAdConfiguration _azureAdConfiguration;

        public TokenProvider(AzureAdConfiguration azureAdConfiguration)
        {
            _azureAdConfiguration = azureAdConfiguration;
        }

        public string GetClientAccessToken(string clientId, string clientSecret, string clientResource)
        {
            var credential = new ClientCredential(clientId, clientSecret);
            var authContext = new AuthenticationContext
            (
                $"{_azureAdConfiguration.AzureAdGraphApiConfig.Authority}{_azureAdConfiguration.AzureAdGraphApiConfig.TenantId}"
            );

            try
            {
                return authContext.AcquireTokenAsync(clientResource, credential).Result.AccessToken;
            }
            catch (AdalException)
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}