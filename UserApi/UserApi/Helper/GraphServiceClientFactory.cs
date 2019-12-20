using Microsoft.Graph;
using System.Net.Http.Headers;

namespace UserApi.Helper
{
    public class GraphServiceClientFactory
    {
        private readonly IGraphApiSettings _graphApiSettings;

        public GraphServiceClientFactory(IGraphApiSettings graphApiSettings)
        {
            _graphApiSettings = graphApiSettings;
        }

        public GraphServiceClient GetAuthenticatedClient()
        {
            return new GraphServiceClient( 
                new DelegateAuthenticationProvider(
                    async requestMessage =>
                    {
                        var accessToken = _graphApiSettings.AccessToken;
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                    }));
        }
    }
}
