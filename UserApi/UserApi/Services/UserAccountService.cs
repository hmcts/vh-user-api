using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using UserApi.Common;
using UserApi.Contract.Requests;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public class UserAccountService : IUserAccountService
    {
        private const string OdataType = "@odata.type";
        private const string GraphGroupType = "#microsoft.graph.group";
        private readonly TimeSpan _retryTimeout;
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly IIdentityServiceApiClient _client;

        public UserAccountService(ISecureHttpRequest secureHttpRequest, IGraphApiSettings graphApiSettings, IIdentityServiceApiClient client)
        {
            _retryTimeout = TimeSpan.FromSeconds(60);
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _client = client;
        }

        public async Task<NewAdUserAccount> CreateUser(string firstName, string lastName, string recoveryEmail)
        {
            var username = await CheckForNextAvailableUsername(firstName, lastName);
            var displayName = $"{firstName} {lastName}";
            await _client.CreateUser(username, firstName, lastName, displayName, recoveryEmail);
            var user = await GetUserById(username);
            
            // Temporarily, until users have stopped requesting it, return the user id for AD as well as username
            return new NewAdUserAccount
            {
                Username = username,
                UserId = user.Id,
                OneTimePassword = "Password123"
            };
        }

        public async Task AddUserToGroup(User user, Group group)
        {
            var body = new CustomDirectoryObject
            {
                ObjectDataId = $"{_graphApiSettings.GraphApiBaseUri}v1.0/directoryObjects/{user.Id}"
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(body));
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}beta/groups/{group.Id}/members/$ref";
            var responseMessage = await _secureHttpRequest.PostAsync(_graphApiSettings.AccessToken, stringContent, accessUri);
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
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/users/{userId}";
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode) return await responseMessage.Content.ReadAsAsync<User>();

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return null;

            var message = $"Failed to get user by id {userId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<User> GetUserByFilter(string filter)
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}/users?$filter={filter}&api-version=1.6";
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessTokenWindows, accessUri);
            
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
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq '{groupName}'";
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);
            
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
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups/{groupId}";
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode) return await responseMessage.Content.ReadAsAsync<Group>();

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return null;

            var message = $"Failed to get group by id {groupId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<List<Group>> GetGroupsForUser(string userId)
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/users/{userId}/memberOf";

            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

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

            if (responseMessage.StatusCode == HttpStatusCode.NotFound) return new List<Group>();

            var message = $"Failed to get group for user {userId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        /// <summary>
        /// Determine the next available username for a participant based on username format [firstname].[lastname]
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns>next available user principal name</returns>
        public async Task<string> CheckForNextAvailableUsername(string firstName, string lastName)
        {
            var domain = "@hearings.reform.hmcts.net";
            var baseUsername = $"{firstName}.{lastName}".ToLower();
            var users = await GetUsersMatchingName(firstName, lastName);
            var lastUserPrincipalName = users.LastOrDefault();
            if (lastUserPrincipalName == null)
            {
                return baseUsername + domain;
            }

            lastUserPrincipalName = GetStringWithoutWord(lastUserPrincipalName, domain);
            lastUserPrincipalName = GetStringWithoutWord(lastUserPrincipalName, baseUsername);
            lastUserPrincipalName = string.IsNullOrEmpty(lastUserPrincipalName) ? "0" : lastUserPrincipalName;
            var lastNumber = int.Parse(lastUserPrincipalName);
            lastNumber += 1;
            return $"{baseUsername}{lastNumber}{domain}";
        }

        private async Task<IEnumerable<string>> GetUsersMatchingName(string firstName, string lastName)
        {
            var baseUsername = $"{firstName}.{lastName}".ToLower();
            var users = await _client.GetUsernamesStartingWith(baseUsername);
            return users.OrderBy(username => username);
        }

        private async Task UpdateAuthenticationInformation(string userId, string recoveryMail, DateTime timeout)
        {
            var model = new UpdateAuthenticationInformationRequest
            {
                OtherMails = new List<string> { recoveryMail }
            };
            var stringContent = new StringContent(JsonConvert.SerializeObject(model));

            var accessUri = $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}/users/{userId}?api-version=1.6";
            var responseMessage = await _secureHttpRequest.PatchAsync(_graphApiSettings.AccessTokenWindows, stringContent, accessUri);
            
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