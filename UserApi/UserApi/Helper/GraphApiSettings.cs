using UserApi.Common;
using UserApi.Security;

namespace UserApi.Helper
{
    public interface IGraphApiSettings
    {
        string GraphApiUriWindows { get; }
        string GraphApiUri { get; }
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

        public string GraphApiUriWindows => "https://graph.windows.net/";

        public string TenantId => _azureAdConfiguration.TenantId;

        public string AccessToken =>
            _tokenProvider.GetClientAccessToken(_azureAdConfiguration.ClientId,
                _azureAdConfiguration.ClientSecret,
                _azureAdConfiguration.GraphApiUri);

        public string AccessTokenWindows =>
            _tokenProvider.GetClientAccessToken(_azureAdConfiguration.ClientId,
                _azureAdConfiguration.ClientSecret,
                GraphApiUriWindows);

        public string GraphApiUri => _azureAdConfiguration.GraphApiUri;
    }
}
