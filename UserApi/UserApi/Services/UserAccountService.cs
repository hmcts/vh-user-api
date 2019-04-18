using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UserApi.Common;
using UserApi.Contract.Requests;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public interface IUserAccountService
    {
        Task<NewAdUserAccount> CreateUser(string firstName, string lastName, string displayName = null,
            string password = null);

        Task AddUserToGroup(User user, Group group);
        Task UpdateAuthenticationInformation(string userId, string recoveryMail);

        /// <summary>
        ///     Get a user in AD either via Object ID or UserPrincipalName
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>The User.</returns>
        Task<User> GetUserById(string userId);

        Task<Group> GetGroupByName(string groupName);
        Task<Group> GetGroupById(string groupId);
        Task<List<Group>> GetGroupsForUser(string userId);
        Task<User> GetUserByFilter(string filter);
    }

    public class UserAccountService : IUserAccountService
    {
        private const string OdataType = "@odata.type";
        private const string GraphGroupType = "#microsoft.graph.group";
        private readonly TimeSpan _retryTimeout;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IGraphApiSettingsHelper _graphApiSettingsHelper;

        public UserAccountService(IHttpClientHelper httpClientHelper, IGraphApiSettingsHelper graphApiSettingsHelper)
        {
            _retryTimeout = TimeSpan.FromSeconds(60);
            _httpClientHelper = httpClientHelper;
            _graphApiSettingsHelper = graphApiSettingsHelper;
        }

        public async Task<NewAdUserAccount> CreateUser(string firstName, string lastName, string displayName = null,
            string password = null)
        {
            const string createdPassword = "Password123";
            var userDisplayName = displayName ?? $"{firstName} {lastName}";

            var userPrincipalName = CheckForNextAvailableUsername(firstName, lastName);

            var user = new User
            {
                AccountEnabled = true,
                DisplayName = userDisplayName,
                MailNickname = $"{firstName}.{lastName}",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = createdPassword
                },
                GivenName = firstName,
                Surname = lastName,
                UserPrincipalName = userPrincipalName
            };

            return await CreateUser(user);
        }

        public async Task AddUserToGroup(User user, Group group)
        {
            var body = new CustomDirectoryObject
            {
                ObjectDataId = $"{_graphApiSettingsHelper.GraphApiBaseUri}v1.0/directoryObjects/{user.Id}"
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(body));
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUri}beta/groups/{group.Id}/members/$ref";
            var responseMessage = await _httpClientHelper.CreateHttpClientPatchOrPostAsync(_graphApiSettingsHelper.AccessToken, stringContent, accessUri, HttpMethod.Post);
            if (responseMessage.IsSuccessStatusCode) return;

            var message = $"Failed to add user {user.Id} to group {group.Id}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task UpdateAuthenticationInformation(string userId, string recoveryMail)
        {
            var timeout = DateTime.Now.Add(_retryTimeout);
            await UpdateAuthenticationInformation(userId, recoveryMail, timeout);
        }

        public async Task<User> GetUserById(string userId)
        {
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUri}v1.0/users/{userId}";
            var responseMessage = await _httpClientHelper.CreateHttpClientGetAsync(_graphApiSettingsHelper.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode) return await responseMessage.Content.ReadAsAsync<User>();

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return null;

            var message = $"Failed to get user by id {userId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<User> GetUserByFilter(string filter)
        {
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUriWindows}{_graphApiSettingsHelper.TenantId}/users?$filter={filter}&api-version=1.6";
            var responseMessage = await _httpClientHelper.CreateHttpClientGetAsync(_graphApiSettingsHelper.AccessTokenWindows, accessUri);
            
            if (responseMessage.IsSuccessStatusCode)
            {
                var queryResponse = await responseMessage.Content
                    .ReadAsAsync<AzureAdGraphQueryResponse<AzureAdGraphUserResponse>>();
                if (!queryResponse.Value.Any()) return null;

                var adUser = queryResponse.Value.First();
                return new User
                {
                    Id = adUser.ObjectId,
                    DisplayName = adUser.DisplayName,
                    UserPrincipalName = adUser.UserPrincipalName,
                    GivenName = adUser.GivenName,
                    Surname = adUser.Surname,
                    Mail = adUser.OtherMails?.FirstOrDefault()
                };
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return null;

            var message = $"Failed to search user with filter {filter}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<Group> GetGroupByName(string groupName)
        {
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUri}v1.0/groups?$filter=displayName eq '{groupName}'";
            var responseMessage = await _httpClientHelper.CreateHttpClientGetAsync(_graphApiSettingsHelper.AccessToken, accessUri);
            
            if (responseMessage.IsSuccessStatusCode)
            {
                var queryResponse = await responseMessage.Content.ReadAsAsync<GraphQueryResponse>();
                return queryResponse.Value?.FirstOrDefault();
            }

            var message = $"Failed to get group by name {groupName}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<Group> GetGroupById(string groupId)
        {
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUri}v1.0/groups/{groupId}";
            var responseMessage = await _httpClientHelper.CreateHttpClientGetAsync(_graphApiSettingsHelper.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode) return await responseMessage.Content.ReadAsAsync<Group>();

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return null;

            var message = $"Failed to get group by id {groupId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<List<Group>> GetGroupsForUser(string userId)
        {
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUri}v1.0/users/{userId}/memberOf";

            var responseMessage = await _httpClientHelper.CreateHttpClientGetAsync(_graphApiSettingsHelper.AccessToken, accessUri);
          
            if (responseMessage.IsSuccessStatusCode)
            {
                var queryResponse = await responseMessage.Content.ReadAsAsync<DirectoryObject>();
                var groupArray = JArray.Parse(queryResponse?.AdditionalData["value"].ToString());

                var groups = new List<Group>();
                foreach (var item in groupArray.Children())
                {
                    var itemProperties = item.Children<JProperty>();
                    var type = itemProperties.FirstOrDefault(x => x.Name == OdataType);

                    // If #microsoft.graph.directoryRole ignore the group mappings
                    if (type.Value.ToString() == GraphGroupType)
                    {
                        var group = JsonConvert.DeserializeObject<Group>(item.ToString());
                        groups.Add(group);
                    }
                }

                return groups;
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return null;

            var message = $"Failed to get group for user {userId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        /// <summary>
        /// Query Graph for users with the same first and last names as existing user.
        /// </summary>
        /// <param name="filter">The filter</param>
        /// <returns>List of users</returns>
        public IList<User> QueryUsers(string filter)
        {
            var queryUrl = $"{_graphApiSettingsHelper.GraphApiBaseUriWindows}{_graphApiSettingsHelper.TenantId}/users?$filter={filter}&api-version=1.6";

            var response = _httpClientHelper.CreateHttpClientGet(_graphApiSettingsHelper.AccessTokenWindows, queryUrl);
            return response.IsSuccessStatusCode
                    ? response.Content.ReadAsAsync<AzureAdGraphQueryResponse<User>>().Result.Value
                   : new List<User>();
        }

        /// <summary>
        /// Determine the next available username for a participant based on username format [firstname].[lastname]
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns>next available user principal name</returns>
        public virtual string CheckForNextAvailableUsername(string firstName, string lastName)
        {
            var baseUsername = $"{firstName}.{lastName}".ToLower();
            var userFilter = $"startswith(userPrincipalName,'{baseUsername}')";
            var users = QueryUsers(userFilter).ToList();
            var domain = "@hearings.reform.hmcts.net";
            if (!users.Any())
            {
                return $"{baseUsername}{domain}";
            }

            users = users.OrderBy(x => x.UserPrincipalName).ToList();
            var lastUserPrincipalName = users.Last().UserPrincipalName;

            lastUserPrincipalName = GetStringWithoutWord(lastUserPrincipalName, domain);
            lastUserPrincipalName = GetStringWithoutWord(lastUserPrincipalName, baseUsername);
            lastUserPrincipalName = string.IsNullOrEmpty(lastUserPrincipalName) ? "0" : lastUserPrincipalName;
            var lastNumber = int.Parse(lastUserPrincipalName);
            lastNumber += 1;
            return $"{baseUsername}{lastNumber}{domain}";
        }

        private async Task<NewAdUserAccount> CreateUser(User newUser)
        {
            var stringContent = new StringContent(JsonConvert.SerializeObject(newUser));
            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUri}v1.0/users";
            var responseMessage = await _httpClientHelper.CreateHttpClientPatchOrPostAsync(_graphApiSettingsHelper.AccessToken, stringContent, accessUri, HttpMethod.Post);

            if (responseMessage.IsSuccessStatusCode)
            {
                var user = await responseMessage.Content.ReadAsAsync<User>();
                var adUserAccount = new NewAdUserAccount
                {
                    Username = user.UserPrincipalName,
                    OneTimePassword = newUser.PasswordProfile.Password,
                    UserId = user.Id
                };
                return adUserAccount;
            }

            var message = $"Failed to add create user {newUser.UserPrincipalName}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        private async Task UpdateAuthenticationInformation(string userId, string recoveryMail, DateTime timeout)
        {
            var model = new UpdateAuthenticationInformationRequest
            {
                OtherMails = new List<string> { recoveryMail }
            };
            var stringContent = new StringContent(JsonConvert.SerializeObject(model));

            var accessUri = $"{_graphApiSettingsHelper.GraphApiBaseUriWindows}{_graphApiSettingsHelper.TenantId}/users/{userId}?api-version=1.6";
            var responseMessage = await _httpClientHelper.CreateHttpClientPatchOrPostAsync(_graphApiSettingsHelper.AccessTokenWindows, stringContent, accessUri, HttpMethod.Patch);
            
            if (responseMessage.IsSuccessStatusCode) return;

            var reason = await responseMessage.Content.ReadAsStringAsync();

            // If it's 404 try it again as the user might simply not have become "ready" in AD
            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                if (DateTime.Now > timeout)
                    throw new UserServiceException("Timed out trying to update alternative address for ${userId}",
                        reason);

                ApplicationLogger.Trace("APIFailure", "GraphAPI 404 PATCH /users/{id}",
                    $"Failed to update authentication information for user {userId}, will retry.");
                await UpdateAuthenticationInformation(userId, recoveryMail, timeout);
                return;
            }

            var message = $"Failed to update alternative email address for {userId}";
            throw new UserServiceException(message, reason);
        }

        private string GetStringWithoutWord(string currentWord, string wordToRemove)
        {
            return currentWord.Remove(currentWord.IndexOf(wordToRemove, StringComparison.InvariantCultureIgnoreCase),
                wordToRemove.Length);
        }
    }
}