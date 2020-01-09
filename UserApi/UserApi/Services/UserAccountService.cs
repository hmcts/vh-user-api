using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public class UserAccountService : IUserAccountService
    {
        private const string OdataType = "@odata.type";
        private const string GraphGroupType = "#microsoft.graph.group";
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly IIdentityServiceApiClient _client;
        private readonly bool _isLive;
        private const string JudgesGroup = "VirtualRoomJudge";
        private const string JudgesTestGroup = "TestAccount";

        private static readonly Compare<UserResponse> CompareJudgeById =
            Compare<UserResponse>.By((x, y) => x.Email == y.Email, x => x.Email.GetHashCode());

        public UserAccountService(ISecureHttpRequest secureHttpRequest, IGraphApiSettings graphApiSettings, IIdentityServiceApiClient client, Settings settings)
        {
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _client = client;
            _isLive = settings.IsLive;
        }

        public async Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail)
        {
            var recoveryEmailText = recoveryEmail.Replace("'", "''");
            var filter = $"otherMails/any(c:c eq '{recoveryEmailText}')";
            var user = await GetUserByFilterAsync(filter);
            if (user != null)
            {
                // Avoid including the exact email to not leak it to logs
                throw new UserExistsException("User with recovery email already exists", user.UserPrincipalName);
            }

            var username = await CheckForNextAvailableUsernameAsync(firstName, lastName);
            var displayName = $"{firstName} {lastName}";
            return await _client.CreateUserAsync(username, firstName, lastName, displayName, recoveryEmail);
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

            // if we failed because the user is already in the group, consider it done anyway
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
                if (!queryResponse.Value.Any())
                {
                    return null;
                }

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
                    if (type.Value.ToString() == GraphGroupType)
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
            var baseUsername = $"{firstName}.{lastName}".ToLowerInvariant();
            var username = new IncrementingUsername(baseUsername, "hearings.reform.hmcts.net");
            var existingUsernames = await GetUsersMatchingNameAsync(baseUsername);
            return username.GetGivenExistingUsers(existingUsernames);
        }

        private async Task<IEnumerable<string>> GetUsersMatchingNameAsync(string baseUsername)
        {
            var users = await _client.GetUsernamesStartingWithAsync(baseUsername);
            return users.OrderBy(username => username);
        }

        public async Task<List<UserResponse>> GetJudgesAsync()
        {
            var judges = await GetJudgesByGroupNameAsync(JudgesGroup);
            if (_isLive)
            {
                judges = await ExcludeTestJudgesAsync(judges);
            }

            return judges.OrderBy(x=>x.DisplayName).ToList();
        }
        private async Task<List<UserResponse>> GetJudgesByGroupNameAsync(string groupName)
        {
            var groupData = await GetGroupByNameAsync(groupName);
            if (groupData == null)
            {
                return new List<UserResponse>();
            }

            var response = await GetJudgesAsync(groupData.Id);
            return response.Select(x => new UserResponse
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                DisplayName = x.DisplayName
            }).ToList();
        }
        private async Task<List<UserResponse>> ExcludeTestJudgesAsync(List<UserResponse> judgesList)
        {
            var testJudges = await GetJudgesByGroupNameAsync(JudgesTestGroup);
            return judgesList.Except(testJudges, CompareJudgeById).ToList();
        }
        private async Task<List<UserResponse>> GetJudgesAsync(string groupId)
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups/{groupId}/members?$top=999";

            var responseMessage = await _secureHttpRequest.GetAsync(_graphApiSettings.AccessToken, accessUri);

            if (responseMessage.IsSuccessStatusCode)
            {
                var queryResponse = await responseMessage.Content.ReadAsAsync<DirectoryObject>();
                var response = JsonConvert.DeserializeObject<List<User>>(queryResponse.AdditionalData["value"].ToString());
                return response.Select(x => new UserResponse
                {
                    FirstName = x.GivenName,
                    LastName = x.Surname,
                    DisplayName = x.DisplayName,
                    Email = x.UserPrincipalName
                }).ToList();
            }

            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var message = $"Failed to get users for group {groupId}";
            var reason = await responseMessage.Content.ReadAsStringAsync();
            throw new UserServiceException(message, reason);
        }
    }
}
