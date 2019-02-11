using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using UserApi.Common;

namespace UserApi.Security
{
    public class AddBearerTokenHeaderHandler : DelegatingHandler
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly AzureAdConfiguration _azureAdConfiguration;
        private const string TokenKey = "s2stoken";

        public AddBearerTokenHeaderHandler(ITokenProvider tokenProvider, IMemoryCache memoryCache,
            IOptions<AzureAdConfiguration> azureAdConfiguration)
        {
            _tokenProvider = tokenProvider;
            _memoryCache = memoryCache;
            _azureAdConfiguration = azureAdConfiguration.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = _memoryCache.Get<string>(TokenKey);
            if (string.IsNullOrEmpty(token))
            {
                var authenticationResult = _tokenProvider.GetAuthorisationResult(_azureAdConfiguration.ClientId,
                    _azureAdConfiguration.ClientSecret, _azureAdConfiguration.VhBookingsApiResourceId);

                token = authenticationResult.AccessToken;
                var tokenExpireDateTime = authenticationResult.ExpiresOn.DateTime.AddMinutes(-1);
                _memoryCache.Set(TokenKey, token, tokenExpireDateTime);
            }

            request.Headers.Add("Authorization", $"Bearer {token}");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}