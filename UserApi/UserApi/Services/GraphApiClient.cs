using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UserApi.Extensions;
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

        public async Task<IEnumerable<string>> GetUsernamesStartingWithAsync(string usernameBase, string contactEmail = null,
            string firstName = null, string lastName = null)
        {
            var filterText = usernameBase.Replace("'", "''");
            var filter = $"startswith(userPrincipalName,'{filterText}')";
            var queryUrl = $"{_baseUrl}/users?$filter={filter}";
            var accessToken = await _graphApiSettings.GetAccessToken();
            var response = await _secureHttpRequest.GetAsync(accessToken, queryUrl);
            await AssertResponseIsSuccessful(response);

            var result = await response.Content.ReadAsAsync<GraphQueryResponse<User>>();
            var existingMatchedUsers = result.Value.Select(user => user.UserPrincipalName).ToList();

            if(!string.IsNullOrEmpty(contactEmail))
            {
                var deletedMatchedUsersByContactMail = await GetDeletedUsersWithPersonalMailAsync(filterText, contactEmail);
                existingMatchedUsers.AddRange(deletedMatchedUsersByContactMail);
            }

            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                var deletedMatchedUsersByPrincipal = await GetDeletedUsersWithNameAsync(filterText, firstName, lastName);
                existingMatchedUsers.AddRange(deletedMatchedUsersByPrincipal);
            }

            return existingMatchedUsers;
        }

        private async Task<List<string>> GetDeletedUsersWithPersonalMailAsync(string usernameBase, string contactMail)
        {
            var queryUrl =
                $"{_baseUrl}/directory/deletedItems/microsoft.graph.user?$filter=startswith(mail, '{contactMail}')";
            return await GetUsernamesWithUri(queryUrl, usernameBase);
        }

        private async Task<List<string>> GetDeletedUsersWithNameAsync(string usernameBase, string firstName,
            string lastName)
        {
            var queryUrl =
                $"{_baseUrl}/directory/deletedItems/microsoft.graph.user?$filter=givenName eq '{firstName}' and surname eq '{lastName}'";
            return await GetUsernamesWithUri(queryUrl, usernameBase);
        }

        private async Task<List<string>> GetUsernamesWithUri(string queryUrl, string usernameBase)
        {
            var accessToken = await _graphApiSettings.GetAccessToken();
            var response = await _secureHttpRequest.GetAsync(accessToken, queryUrl);
            await AssertResponseIsSuccessful(response);
            var deletedMatchedUsers = await response.Content.ReadAsAsync<GraphQueryResponse<User>>();
            List<string> usernames = new();
            foreach (var username in deletedMatchedUsers.Value.Select(u => u.UserPrincipalName))
            {
                var basePrincipal = username.ExtractBasePrincipalName(usernameBase);
                if (!string.IsNullOrEmpty(basePrincipal))
                {
                    usernames.Add(basePrincipal);
                }
            }

            return usernames;
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
                mail = recoveryEmail,
                otherMails = new List<string> { recoveryEmail },
                accountEnabled = true,
                userPrincipalName = username,
                passwordProfile = new
                {
                    forceChangePasswordNextSignIn = !isTestUser,
                    password = newPassword
                },
                userType = UserType.Guest
            };

            var json = JsonConvert.SerializeObject(user);
            var stringContent = new StringContent(json);
            var accessUri = $"{_baseUrl}/users";
            var accessToken = await _graphApiSettings.GetAccessToken();
            var response = await _secureHttpRequest.PostAsync(accessToken, stringContent, accessUri);
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

        public async Task<User> UpdateUserAccount(string userId, string firstName, string lastName, string newUsername, string contactEmail = null)
        {
            var updatedUser = new User
            {
                Id = userId,
                GivenName = firstName,
                Surname = lastName,
                DisplayName = $"{firstName} {lastName}",
                UserPrincipalName = newUsername
            };
            
            if (!string.IsNullOrEmpty(contactEmail))
            {
                updatedUser.Mail = contactEmail;
                updatedUser.OtherMails = new List<string> { contactEmail };
            }
            
            var json = JsonConvert.SerializeObject(updatedUser);
            var stringContent = new StringContent(json);
            var accessUri = $"{_baseUrl}/users/{userId}";
            var accessToken = await _graphApiSettings.GetAccessToken();
            var response = await _secureHttpRequest.PatchAsync(accessToken, stringContent, accessUri);
            await AssertResponseIsSuccessful(response);
            return updatedUser;
        }

        public async Task DeleteUserAsync(string username)
        {
            var queryUrl = $"{_baseUrl}/users/{username}";
            var accessToken = await _graphApiSettings.GetAccessToken();
            var response = await _secureHttpRequest.DeleteAsync(accessToken, queryUrl);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new UserDoesNotExistException(username);
            }
            await AssertResponseIsSuccessful(response);
        }

        private static async Task AssertResponseIsSuccessful(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                if(response.StatusCode == HttpStatusCode.BadRequest && message.Contains("ObjectConflict"))
                {
                    throw new UserExistsException("User with Mail already exists", "");
                }
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
            var accessToken = await _graphApiSettings.GetAccessToken();
            var response = await _secureHttpRequest.PatchAsync(accessToken, stringContent, accessUri);
            await AssertResponseIsSuccessful(response);

            return newPassword;
        }
    }
}
