using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using UserApi.Helper;
using UserApi.Services.Models;

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
            _baseUrl = $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}";
        }

        public async Task<IEnumerable<string>> GetUsernamesStartingWith(string text)
        {
            var filter = $"startswith(userPrincipalName,'{text}')";
            var queryUrl = $"{_baseUrl}/users?$filter={filter}&api-version=1.6";

            var response = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessTokenWindows, queryUrl);
            var result = await response.Content.ReadAsAsync<AzureAdGraphQueryResponse<User>>();
            return result.Value.Select(user => user.UserPrincipalName);
        }
    }
}