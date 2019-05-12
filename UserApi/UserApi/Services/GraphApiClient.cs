using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UserApi.Helper;
using UserApi.Services.Models;
using User = Microsoft.Graph.User;

namespace UserApi.Services
{
    public class GraphApiClient : IIdentityServiceApiClient {
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly string _baseGraphUrl;
        
        private const string DefaultPassword = "***REMOVED***";

        public GraphApiClient(ISecureHttpRequest secureHttpRequest, IGraphApiSettings graphApiSettings)
        {
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _baseGraphUrl = $"{_graphApiSettings.GraphApiBaseUri}/v1.0/{_graphApiSettings.TenantId}";
        }

        public async Task<IEnumerable<string>> GetUsernamesStartingWith(string text)
        {
            var filter = $"startswith(userPrincipalName,'{text}')";
            var queryUrl = $"{_baseGraphUrl}/users?$filter={filter}";

            var response = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, queryUrl);
            await AssertResponseIsSuccessful(response);

            var result = await response.Content.ReadAsAsync<AzureAdGraphQueryResponse<User>>();
            return result.Value.Select(user => user.UserPrincipalName);
        }

        public async Task<NewAdUserAccount> CreateUser(string username, string firstName, string lastName, string displayName, string recoveryEmail)
        {
            // The new MS Graph API endpoint to create users do not yet allow setting the otherMails property
            // which we require to set the alternative email for self service password reset so we have to use
            // the old Azure AD api to do this until this has been implemented in Graph API
            // https://developer.microsoft.com/en-us/office/blogs/microsoft-graph-or-azure-ad-graph/
            
            // the user object provided by the graph api nuget package is missing the otherMails property
            // but it's there in the API so using a dynamic request model instead
            var user = new
            {
                displayName,
                givenName = firstName,
                surname = lastName,
                mailNickname = $"{firstName}.{lastName}".ToLower(),
                otherMails = new List<string> { recoveryEmail },
                accountEnabled = true,
                userPrincipalName = username,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = true,
                    password = DefaultPassword
                }
            };

            var json = JsonConvert.SerializeObject(user);
            var stringContent = new StringContent(json);
            var accessUri = $"{_baseGraphUrl}/users";
            var response = await _secureHttpRequest.PostAsync(_graphApiSettings.AccessToken, stringContent, accessUri);
            await AssertResponseIsSuccessful(response);
            var responseJson = await response.Content.ReadAsStringAsync();
            var adAccount = JsonConvert.DeserializeObject<User>(responseJson);
            return new NewAdUserAccount
            {
                OneTimePassword = DefaultPassword,
                UserId = adAccount.Id,
                Username = adAccount.UserPrincipalName
            };
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