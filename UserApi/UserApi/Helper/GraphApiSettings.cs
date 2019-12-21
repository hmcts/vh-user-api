using UserApi.Common;
using UserApi.Security;

namespace UserApi.Helper
{
    public interface IGraphApiSettings
    {
        string GraphApiBaseUriWindows { get; }
        string GraphApiBaseUri { get; }
        string AccessToken { get; }
        string AccessTokenWindows { get; }
        string TenantId { get; }
    }

    public class GraphApiSettings: IGraphApiSettings
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly AzureAdConfiguration _azureAdConfiguration;

        public GraphApiSettings(ITokenProvider tokenProvider, AzureAdConfiguration azureAdConfig)
        {
            _tokenProvider = tokenProvider;
            _azureAdConfiguration = azureAdConfig;
        }

        public string GraphApiBaseUriWindows => "https://graph.windows.net/";
        public string GraphApiBaseUri => _azureAdConfiguration.GraphApiBaseUri;
        public string TenantId => _azureAdConfiguration.AzureAdGraphApiConfig.TenantId;

        public string AccessToken => _tokenProvider.GetClientAccessToken
        (
            _azureAdConfiguration.AzureAdGraphApiConfig.ClientId,
            _azureAdConfiguration.AzureAdGraphApiConfig.ClientSecret,
            _azureAdConfiguration.GraphApiBaseUri
        );

        public string AccessTokenWindows => _tokenProvider.GetClientAccessToken
        (
            _azureAdConfiguration.AzureAdGraphApiConfig.ClientId,
            _azureAdConfiguration.AzureAdGraphApiConfig.ClientSecret,
            GraphApiBaseUriWindows
        );
    }
}
