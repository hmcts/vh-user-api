using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UserApi.Helper;
using UserApi.Services.Models;
using User = Microsoft.Graph.User;

namespace UserApi.Services
{
    /// <summary>
    /// Implementation of an identity service
    /// </summary>
    /// <remarks>
    /// There are two versions of the graph api, one newer called MS Graph and the older, Azure AD Graph.
    /// Previously we were (and still are in places) using the Azure AD Graph because of the lack of certain features.
    /// It seems that since the start of 2019 these features are now in place, though not always in the nuget package,
    /// in the new MS Graph API so we need to begin moving over to using that. For this reason this class is implemented
    /// using only the new MS Graph API.
    /// https://developer.microsoft.com/en-us/office/blogs/microsoft-graph-or-azure-ad-graph/
    /// </remarks>
    public class GraphApiClient : IIdentityServiceApiClient {
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly IPasswordService _passwordService;
        private readonly string _baseUrl;
        private readonly string _testDefaultPassword;

        public GraphApiClient(ISecureHttpRequest secureHttpRequest,
            IGraphApiSettings graphApiSettings,
            IPasswordService passwordService,
            Settings settings)
        {
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _passwordService = passwordService;
            _testDefaultPassword = settings.TestDefaultPassword;
            _baseUrl = $"{_graphApiSettings.GraphApiBaseUri}/v1.0/{_graphApiSettings.TenantId}";
        }

        public async Task<IEnumerable<string>> GetUsernamesStartingWithAsync(string text)
        { 
            var filterText = text.Replace("'", "''");
            var filter = $"startswith(userPrincipalName,'{filterText}')";
            var queryUrl = $"{_baseUrl}/users?$filter={filter}";
            var response = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, queryUrl);
            await AssertResponseIsSuccessful(response);

            var result = await response.Content.ReadAsAsync<AzureAdGraphQueryResponse<User>>();
            return result.Value.Select(user => user.UserPrincipalName);
        }

        public async Task<NewAdUserAccount> CreateUserAsync(string username, string firstName, string lastName, string displayName, string recoveryEmail, bool isTestUser = false)
        {
            var periodRegexString = "^\\.|\\.$";

            // the user object provided by the graph api nuget package is missing the otherMails property
            // but it's there in the API so using a dynamic request model instead
            var newPassword = isTestUser ? _testDefaultPassword : _passwordService.GenerateRandomPasswordWithDefaultComplexity();
            var user = new
            {
                displayName,
                givenName = firstName,
                surname = lastName,
                mailNickname = $"{Regex.Replace(firstName, periodRegexString, string.Empty)}.{Regex.Replace(lastName, periodRegexString, string.Empty)}"
                    .ToLower(),
                otherMails = new List<string> { recoveryEmail },
                accountEnabled = true,
                userPrincipalName = username,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = !isTestUser,
                    password = newPassword
                },
                userType = "Guest"
            };

            var json = JsonConvert.SerializeObject(user);
            var stringContent = new StringContent(json);
            var accessUri = $"{_baseUrl}/users";
            var response = await _secureHttpRequest.PostAsync(_graphApiSettings.AccessToken, stringContent, accessUri);
            await AssertResponseIsSuccessful(response);
            var responseJson = await response.Content.ReadAsStringAsync();
            var adAccount = JsonConvert.DeserializeObject<User>(responseJson);
            return new NewAdUserAccount
            {
                OneTimePassword = newPassword,
                UserId = adAccount.Id,
                Username = adAccount.UserPrincipalName
            };
        }

        public async Task<User> UpdateUserAccount(string userId, string firstName, string lastName, string newUsername)
        {
            var updatedUser = new User
            {
                Id = userId,
                GivenName = firstName,
                Surname = lastName,
                DisplayName = $"{firstName} {lastName}",
                UserPrincipalName = newUsername
            };
            
            var json = JsonConvert.SerializeObject(updatedUser);
            var stringContent = new StringContent(json);
            var accessUri = $"{_baseUrl}/users/{userId}";
            var response = await _secureHttpRequest.PatchAsync(_graphApiSettings.AccessToken, stringContent, accessUri);
            await AssertResponseIsSuccessful(response);
            return updatedUser;
        }

        public async Task DeleteUserAsync(string username)
        {
            var queryUrl = $"{_baseUrl}/users/{username}";
            var response = await _secureHttpRequest.DeleteAsync(_graphApiSettings.AccessToken, queryUrl);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new UserDoesNotExistException(username);
            }
            await AssertResponseIsSuccessful(response);
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

        public async Task<string> UpdateUserPasswordAsync(string username)
        {
            var newPassword = _passwordService.GenerateRandomPasswordWithDefaultComplexity();
            
            var user = new
            {    
                userPrincipalName = username,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = true,
                    password = newPassword
                }
            };

            var json = JsonConvert.SerializeObject(user);
            var stringContent = new StringContent(json);
            var accessUri = $"{_baseUrl}/users/{username}";
            var response = await _secureHttpRequest.PatchAsync(_graphApiSettings.AccessToken, stringContent, accessUri);
            await AssertResponseIsSuccessful(response);

            return newPassword;
        }
    }
}
