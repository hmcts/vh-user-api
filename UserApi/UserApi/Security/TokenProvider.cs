using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using UserApi.Common;

namespace UserApi.Security
{
    public interface ITokenProvider
    {
        Task<string> GetClientAccessToken(string clientId, string clientSecret, string clientResource);
        Task<AuthenticationResult> GetAuthorisationResult(string clientId, string clientSecret, string clientResource);
    }

    public class TokenProvider(AzureAdConfiguration azureAdConfiguration) : ITokenProvider
    {
        public async Task<string> GetClientAccessToken(string clientId, string clientSecret, string clientResource)
        {
            var result = await GetAuthorisationResult(clientId, clientSecret, clientResource);
            return result.AccessToken;
        }

        public async Task<AuthenticationResult> GetAuthorisationResult(string clientId, string clientSecret, string clientResource)
        {
            var authority = $"{azureAdConfiguration.Authority}{azureAdConfiguration.TenantId}";
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            var scopes = new[] { $"{clientResource}/.default" };

            try
            {
                return await app.AcquireTokenForClient(scopes).ExecuteAsync();
            }
            catch (MsalServiceException)
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}