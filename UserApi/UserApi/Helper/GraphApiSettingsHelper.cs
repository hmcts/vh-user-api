using Microsoft.Extensions.Options;
using UserApi.Common;
using UserApi.Security;

namespace UserApi.Helper
{
    public interface IGraphApiSettingsHelper
    {
        string GraphApiBaseUriWindows { get; }
        string GraphApiBaseUri { get; }
        string AccessToken { get; }
        string AccessTokenWindows { get; }
        string TenantId { get; }
    }

    public class GraphApiSettingsHelper: IGraphApiSettingsHelper
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly AzureAdConfiguration _azureAdConfiguration;

        public GraphApiSettingsHelper(ITokenProvider tokenProvider, IOptions<AzureAdConfiguration> azureAdConfigOptions)
        {
            _tokenProvider = tokenProvider;
            _azureAdConfiguration = azureAdConfigOptions.Value;
        }

        public string GraphApiBaseUriWindows
        {
            get { return "https://graph.windows.net/"; }
        }

        public string TenantId
        {
            get { return _azureAdConfiguration.TenantId; }
        }

        public string AccessToken
        {
            get
            {
                return _tokenProvider.GetClientAccessToken(_azureAdConfiguration.ClientId,
                   _azureAdConfiguration.ClientSecret,
                   _azureAdConfiguration.GraphApiBaseUri);
            }
        }
        public string AccessTokenWindows
        {
            get
            {
                return _tokenProvider.GetClientAccessToken(_azureAdConfiguration.ClientId,
                   _azureAdConfiguration.ClientSecret,
                   GraphApiBaseUriWindows);
            }
        }

        public string GraphApiBaseUri
        {
            get
            {
                return _azureAdConfiguration.GraphApiBaseUri;
            }
        }
    }
}
