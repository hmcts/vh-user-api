using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UserApi.Helper;
using UserApi.Services.Models;
using User = Microsoft.Graph.User;

namespace UserApi.Services
{
    public class GraphApiClient : IIdentityServiceApiClient {
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly string _baseUrl;

        public GraphApiClient(ISecureHttpRequest secureHttpRequest, IGraphApiSettings graphApiSettings)
        {
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _baseUrl = $"{_graphApiSettings.GraphApiBaseUri}/v1.0/{_graphApiSettings.TenantId}";
        }

        public async Task<IEnumerable<string>> GetUsernamesStartingWith(string text)
        { 
            var filterText = text.Replace("'", "''");
            var filter = $"startswith(userPrincipalName,'{filterText}')";
            var queryUrl = $"{_baseUrl}/users?$filter={filter}";
            var response = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, queryUrl);
            await AssertResponseIsSuccessful(response);

            var result = await response.Content.ReadAsAsync<AzureAdGraphQueryResponse<User>>();
            return result.Value.Select(user => user.UserPrincipalName);
        }

        private static async Task AssertResponseIsSuccessful(HttpResponseMessage response)
        {
            // TODO: Move this code into the http request class and have that throw an exception if response type isn't valid
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new IdentityServiceApiException("Failed to call API: " + response.StatusCode + "\r\n" + message);
            }
        }
    }
}