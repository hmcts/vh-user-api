using System;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UserApi.Caching;
using UserApi.Contract.Responses;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services.Models;
using System.Text.RegularExpressions;
using UserApi.Mappers;
using Group = Microsoft.Graph.Group;
using UserApi.Validations;

namespace UserApi.Services
{
    public class UserAccountService : IUserAccountService
    {
        private const string OdataType = "@odata.type";
        private const string GraphGroupType = "#microsoft.graph.group";
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly IIdentityServiceApiClient _client;
        private readonly ICache _distributedCache;
        private readonly Settings _settings;
        private const string PerformanceTestUserFirstName = "TP";
        private const string UserGroupCacheKey = "cachekey.ad.group";

        public static readonly Compare<UserResponse> CompareJudgeById =
            Compare<UserResponse>.By((x, y) => x.Email == y.Email, x => x.Email.GetHashCode());

        public UserAccountService(ISecureHttpRequest secureHttpRequest, IGraphApiSettings graphApiSettings, IIdentityServiceApiClient client,
            Settings settings, ICache distributedCache)
        {
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _client = client;
            _distributedCache = distributedCache;
            _settings = settings;
        }

        public async Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail,
            bool isTestUser)
        {
            if (!recoveryEmail.IsValidEmail())
            {
                throw new InvalidEmailException("Recovery email is not a valid email", recoveryEmail);
            }

            var username = await CheckForNextAvailableUsernameAsync(firstName, lastName);
            var displayName = $"{firstName} {lastName}";

            return await _client.CreateUserAsync(username, firstName, lastName, displayName, recoveryEmail, isTestUser);
        }

        public async Task<User> UpdateUserAccountAsync(Guid userId, string firstName, string lastName)
        {
            var filter = $"objectId  eq '{userId}'";
            var user = await GetUserByFilterAsync(filter);
            if (user == null)
            {
                throw new UserDoesNotExistException(userId);
            }

            var username = user.UserPrincipalName;
            if (!user.GivenName.Equals(firstName, StringComparison.CurrentCultureIgnoreCase) ||
                !user.Surname.Equals(lastName, StringComparison.CurrentCultureIgnoreCase))
            {
                username = await CheckForNextAvailableUsernameAsync(firstName, lastName);
            }

            return await _client.UpdateUserAccount(user.Id, firstName, lastName, username);
        }

        public async Task DeleteUserAsync(string username)
        {
            await _client.DeleteUserAsync(username);
        }

        public async Task AddUserToGroupAsync(User user, Group group)
        {
            var body = new CustomDirectoryObject
            {
                ObjectDataId = $"{_graphApiSettings.GraphApiBaseUri}v1.0/{_graphApiSettings.TenantId}/directoryObjects/{user.Id}"
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(body));
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/{_graphApiSettings.TenantId}/groups/{group.Id}/members/$ref";
            var responseMessage = await _secureHttpRequest.PostAsync(_graphApiSettings.AccessToken, stringContent, accessUri);
            if (responseMessage.IsSuccessStatusCode)
            {
                return;
            }

            var reason = await responseMessage.Content.ReadAsStringAsync();

            // If we failed because the user is already in the group, consider it done anyway
            if (reason.Contains("already exist"))
            {
                return;
            }

            var message = $"Failed to add user {user.Id} to group {group.Id}";
            throw new UserServiceException(message, reason);
        }

        public async Task<User> GetUserByFilterAsync(string filter)
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUriWindows}{_graphApiSettings.TenantId}/users?$filter={filter}&api-version=1.6";
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessTokenWindows, accessUri);

            if (responseMessage.IsSuccessStatusCode)
            {
                var queryResponse = await responseMessage.Content
                    .ReadAsAsync<AzureAdGraphQueryResponse<AzureAdGraphUserResponse>>();
                if (queryResponse != null && queryResponse.Value != null && queryResponse.Value.Any())
                {
                    var adUser = queryResponse.Value[0];
                    return new User
                    {
                        Id = adUser.ObjectId,
                        DisplayName = adUser.DisplayName,
                        UserPrincipalName = adUser.UserPrincipalName,
                        GivenName = adUser.GivenName,
                        Surname = adUser.Surname,
                        Mail = adUser.OtherMails?.FirstOrDefault() ?? adUser.ContactEmail,
                        MobilePhone = adUser.MobilePhone
                    };
                }
                else
                {
                    return null;
                }
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var message = $"Failed to search user with filter {filter}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }

        public async Task<Group> GetGroupByNameAsync(string groupName)
        {
            var group = await _distributedCache.GetOrAddAsync($"{UserGroupCacheKey}.{groupName}", () => GetGraphAdGroupAsync(groupName));

            return group;
        }

        private async Task<Group> GetGraphAdGroupAsync(string groupName)
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

        public async Task<Group> GetGroupByIdAsync(string groupId)
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups/{groupId}";
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode)
            {
                return await responseMessage.Content.ReadAsAsync<Group>();
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var message = $"Failed to get group by id {groupId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();

            throw new UserServiceException(message, reason);
        }

        public async Task<bool> IsUserAdminAsync(string principalId)
        {
            var userRoleAssignmentUri = $"{_graphApiSettings.GraphApiBaseUri}beta/roleManagement/directory/roleAssignments?$filter=principalId eq '{principalId}'";  
            
            var adminRoleUri = $"{_graphApiSettings.GraphApiBaseUri}beta/roleManagement/directory/roleDefinitions?$filter=DisplayName eq 'User Administrator'";

            var userAssignedRoles = (await ExecuteRequest<AzureAdGraphQueryResponse<UserAssignedRole>>(userRoleAssignmentUri))?.Value;

            var adminRole = (await ExecuteRequest<AzureAdGraphQueryResponse<RoleDefinition>>(adminRoleUri))?.Value[0];

            if (userAssignedRoles == null || adminRole == null) return false;

            return userAssignedRoles.Any(r => r.RoleDefinitionId == adminRole?.Id);
        }

        private async Task<T> ExecuteRequest<T>(string accessUri) where T : class
        {
            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode)
            {
                var content = await responseMessage.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(content);
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            throw new UserServiceException($"An error occurred processing request {accessUri}", responseMessage.ReasonPhrase );
        }

        public async Task<List<Group>> GetGroupsForUserAsync(string userId)
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
                    if (type != null && type.Value.ToString() == GraphGroupType)
                    {
                        var group = JsonConvert.DeserializeObject<Group>(item.ToString());
                        groups.Add(group);
                    }
                }

                return groups;
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<Group>();
            }

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
        public async Task<string> CheckForNextAvailableUsernameAsync(string firstName, string lastName)
        {
            var periodRegexString = "^\\.|\\.$";
            var sanitisedFirstName = Regex.Replace(firstName, periodRegexString, string.Empty);
            var sanitisedLastName = Regex.Replace(lastName, periodRegexString, string.Empty);

            sanitisedFirstName = Regex.Replace(sanitisedFirstName, " ", string.Empty);
            sanitisedLastName = Regex.Replace(sanitisedLastName, " ", string.Empty);

            var baseUsername = $"{sanitisedFirstName}.{sanitisedLastName}".ToLowerInvariant();
            var username = new IncrementingUsername(baseUsername, _settings.ReformEmail);
            var existingUsernames = await GetUsersMatchingNameAsync(baseUsername);
            return username.GetGivenExistingUsers(existingUsernames);
        }

        private async Task<IEnumerable<string>> GetUsersMatchingNameAsync(string baseUsername)
        {
            var users = await _client.GetUsernamesStartingWithAsync(baseUsername);
            return users;
        }

        public async Task<IEnumerable<UserResponse>> GetJudgesAsync(string username = null)
        {
            var judges = await GetJudgesByGroupNameAndFilterAsync(_settings.AdGroup.Judge, username);
            judges = ExcludePerformanceTestUsersAsync(judges);

            if (_settings.IsLive)
            {
                judges = await ExcludeTestJudgesAsync(judges);
            }

            return judges.OrderBy(x => x.DisplayName);
        }

        public async Task<IEnumerable<UserResponse>> GetEjudiciaryJudgesAsync(string username)
        {
            var judges = await GetJudgesByGroupNameAndFilterAsync(_settings.AdGroup.JudicialOfficeHolder, username);
            return judges.OrderBy(x => x.DisplayName);
        }

        private async Task<IEnumerable<UserResponse>> GetJudgesByGroupNameAndFilterAsync(string groupName, string filter = null)
        {
            var groupData = await GetGroupByNameAsync(groupName);

            if (groupData == null)
            {
                return Enumerable.Empty<UserResponse>();
            }

            return await GetJudgesAsyncByGroupIdAndUsername(groupData.Id, filter);
        }

        private async Task<IEnumerable<UserResponse>> ExcludeTestJudgesAsync(IEnumerable<UserResponse> judgesList)
        {
            var testJudges = await GetJudgesByGroupNameAndFilterAsync(_settings.AdGroup.JudgesTestGroup);

            return judgesList.Except(testJudges, CompareJudgeById);
        }

        private static IEnumerable<UserResponse> ExcludePerformanceTestUsersAsync(IEnumerable<UserResponse> judgesList)
        {
            return judgesList.Where(u => !string.IsNullOrWhiteSpace(u.FirstName) && !u.FirstName.StartsWith(PerformanceTestUserFirstName));
        }

        private async Task<IEnumerable<UserResponse>> GetJudgesAsyncByGroupIdAndUsername(string groupId, string username = null)
        {
            var users = new List<UserResponse>();

            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups/{groupId}/members/microsoft.graph.user?" + 
                            "$select=id,otherMails,userPrincipalName,displayName,givenName,surname&$top=999";

            while (true)
            {
                var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                    {
                        return Enumerable.Empty<UserResponse>();
                    }

                    var message = $"Failed to get users for group {groupId}";
                    var reason = await responseMessage.Content.ReadAsStringAsync();

                    throw new UserServiceException(message, reason);
                }

                var directoryObject = await responseMessage.Content.ReadAsAsync<DirectoryObject>();
                var response = JsonConvert.DeserializeObject<List<User>>(directoryObject.AdditionalData["value"].ToString());

                users.AddRange(response
                    .Where(x => username != null && x.UserPrincipalName.ToLower().Contains(username.ToLower()) || string.IsNullOrEmpty(username))
                    .Select(GraphUserMapper.MapToUserResponse));

                if (!directoryObject.AdditionalData.ContainsKey("@odata.nextLink"))
                {
                    return users;
                }

                accessUri = directoryObject.AdditionalData["@odata.nextLink"].ToString();
            }
        }

        public async Task<string> UpdateUserPasswordAsync(string username)
        {
            return await _client.UpdateUserPasswordAsync(username);
        }
    }
}
