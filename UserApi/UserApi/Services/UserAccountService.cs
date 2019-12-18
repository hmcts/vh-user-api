using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.Services
{
    public class UserAccountService : IUserAccountService
    {
        private const string OdataType = "@odata.type";
        private const string GraphGroupType = "#microsoft.graph.group";
        private const string GraphUserType = "#microsoft.graph.user";
        private readonly ISecureHttpRequest _secureHttpRequest;
        private readonly IGraphApiSettings _graphApiSettings;
        private readonly IIdentityServiceApiClient _client;
        private readonly IGraphServiceClient _graphClient;
        private readonly bool _isLive;
        private const string JudgesGroup = "VirtualRoomJudge";
        private const string JudgesTestGroup = "TestAccount";
        private readonly string _defaultPassword;

        private static readonly Compare<User> CompareJudgeById =
            Compare<User>.By((x, y) => x.Mail == y.Mail, x => x.Mail.GetHashCode());

        public UserAccountService(ISecureHttpRequest secureHttpRequest, IGraphApiSettings graphApiSettings, IIdentityServiceApiClient client, Settings settings, IGraphServiceClient graphServiceClient)
        {
            _secureHttpRequest = secureHttpRequest;
            _graphApiSettings = graphApiSettings;
            _graphClient = graphServiceClient;
            _defaultPassword = settings.DefaultPassword;
            _client = client;
            _isLive = settings.IsLive;
        }

        public async Task<NewAdUserAccount> CreateUserAsync(string firstName, string lastName, string recoveryEmail)
        {
            var filter = $"otherMails/any(c:c eq '{recoveryEmail}')";
            var user = await GetUserByFilterAsync(filter);
            if (user != null)
            {
                // Avoid including the exact email to not leak it to logs
                throw new UserExistsException("User with recovery email already exists", user.UserPrincipalName);
            }

            var username = await CheckForNextAvailableUsernameAsync(firstName, lastName);
            var displayName = $"{firstName} {lastName}";

            var newUser = await _graphClient.Users.Request().AddAsync(new User
            {
                DisplayName = displayName,
                GivenName = firstName,
                Surname = lastName,
                MailNickname = $"{firstName}.{lastName}".ToLower(),
                OtherMails = new List<string> { recoveryEmail },
                AccountEnabled = true,
                UserPrincipalName = username,
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = _defaultPassword
                }
            });

            return new NewAdUserAccount
            {
                OneTimePassword = _defaultPassword,
                UserId = newUser.Id,
                Username = newUser.UserPrincipalName
            };
        }

        public async Task AddUserToGroupAsync(User user, Group group)
        {
            var directoryObject = new DirectoryObject
            {
                Id = user.Id
            };

            try
            {
                await _graphClient.Groups[group.Id].Members.References.Request().AddAsync(directoryObject);
            }
            catch (ServiceException ex)
            {
                if (ex.Message.Contains("One or more added object references already exist for the following modified properties: 'members'"))
                {
                    return;
                }
                else
                {
                    var message = $"Failed to add user {user.Id} to group {group.Id}";
                    throw new UserServiceException(message, ex.Message);
                }
            }
            catch (Exception ex)
            {
                var message = $"Failed to add user {user.Id} to group {group.Id}";
                throw new UserServiceException(message, ex.Message);
            }
        }

        public async Task<User> GetUserByFilterAsync(string filter)
        {
            try
            {
                var users = await _graphClient.Users.Request().Select(u => new
                {
                    u.Id,
                    u.DisplayName,
                    u.UserPrincipalName,
                    u.GivenName,
                    u.Surname,
                    u.OtherMails,
                    u.MemberOf,
                    u.TransitiveMemberOf
                })
                .Filter(filter).GetAsync();

                return new User
                {
                    Id = users.First().Id,
                    DisplayName = users.First().DisplayName,
                    UserPrincipalName = users.First().UserPrincipalName,
                    GivenName = users.First().GivenName,
                    Surname = users.First().Surname,
                    Mail = users.First().OtherMails?.FirstOrDefault()
                };

            }
            catch (Exception ex)
            {
                var message = $"Failed to search user with filter {filter}";
                throw new UserServiceException(message, ex.Message);
            }
        }

        public async Task<Group> GetGroupByNameAsync(string groupName)
        {
            try
            {
                var group = await _graphClient.Groups.Request().Filter($"displayName eq '{groupName}'").GetAsync();

                return group.FirstOrDefault();
            }
            catch (Exception ex)
            {
                var message = $"Failed to get group by name {groupName}";
                throw new UserServiceException(message, ex.Message);
            }
        }

        public async Task<Group> GetGroupByIdAsync(string groupId)
        {
            try
            {
                var group = await _graphClient.Groups[groupId].Request().GetAsync();

                return group;
            }
            catch (Exception ex)
            {
                var message = $"Failed to get group by id {groupId}";
                throw new UserServiceException(message, ex.Message);
            }
        }

        public async Task<List<Group>> GetGroupsForUserAsync(string userId)
        {

            try
            {
                var memberships = await _graphClient.Users[userId].TransitiveMemberOf.Request().GetAsync();

                var groups = new List<Group>();
                foreach (Group group in memberships.Where(m => m.ODataType == GraphGroupType))
                {
                    groups.Add(group);
                }

                return groups;
            }
            catch (Exception ex)
            {
                var message = $"Failed to get group for user {userId}";
                throw new UserServiceException(message, ex.Message);
            }
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
            var filterText = baseUsername.Replace("'", "''");
            var filter = $"startswith(userPrincipalName,'{filterText}')";
            var users = await _graphClient.Users.Request().Select(u => new
            {
                u.UserPrincipalName
            })
                .OrderBy("UserPrincipalName")
                .Filter(filter).GetAsync();

            return users.Select(u => u.UserPrincipalName);
        }

        public async Task<List<User>> GetJudgesAsync()
        {
            var judges = await GetJudgesByGroupNameAsync(JudgesGroup);
            if (_isLive)
            {
                judges = await ExcludeTestJudgesAsync(judges);
            }

            return judges.OrderBy(x => x.DisplayName).ToList();
        }
        private async Task<List<User>> GetJudgesByGroupNameAsync(string groupName)
        {
            var groupData = await GetGroupByNameAsync(groupName);
            if (groupData == null)
            {
                return new List<User>();
            }

            var judges = await GetJudgesAsync(groupData.Id);
            return judges.ToList();
        }
        private async Task<List<User>> ExcludeTestJudgesAsync(List<User> judgesList)
        {
            var testJudges = await GetJudgesByGroupNameAsync(JudgesTestGroup);
            return judgesList.Except(testJudges, CompareJudgeById).ToList();
        }
        private async Task<IEnumerable<User>> GetJudgesAsync(string groupId)
        {
            try
            {
                var judgeGroup = await _graphClient.Groups.Request().Filter($"displayName eq '{groupId}'").GetAsync();

                if (judgeGroup.Count == 0)
                {
                    throw new UserServiceException("Failed to get Judges", "No Judge group found");
                }

                var judges = await _graphClient.Groups[judgeGroup.First().Id].TransitiveMembers.Request().GetAsync();

                return judges.Where(m => m.ODataType == GraphUserType).Cast<User>();
            }
            catch (Exception ex)
            {
                var message = $"Failed to get judges for group {groupId}";
                throw new UserServiceException(message, ex.Message);
            }
        }
    }
}
