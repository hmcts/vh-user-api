
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
            GraphServiceClient graphClient = new GraphServiceClient( 
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        string accessToken = _graphApiSettings.AccessToken;

                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                    }));
            return graphClient;
        }
    }
}
