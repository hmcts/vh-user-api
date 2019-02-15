using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using UserApi.Security;

namespace UserApi.Services
{
    public class UserManager
    {
        private readonly IUserAccountService _userAccountService;

        public UserManager(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        /// <summary>
        /// Create a user account in AD and assign the roles and set the recovery information.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="recoveryEmail"></param>
        /// <param name="role"></param>
        /// <returns>The username</returns>
        public virtual async Task<string> CreateAdAccount(string firstName, string lastName, string recoveryEmail,
            string role)
        {
            var userPrincipalName = await CheckForNextAvailableUsername(firstName, lastName);

            var user = new User
            {
                AccountEnabled = true,
                DisplayName = $@"{firstName} {lastName}",
                MailNickname = $@"{firstName}.{lastName}",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = "Password123"
                },
                GivenName = firstName,
                Surname = lastName,
                UserPrincipalName = userPrincipalName
            };

            var adUser = await _userAccountService.CreateUser(user);
            _userAccountService.UpdateAuthenticationInformation(adUser.UserId, recoveryEmail);
            await AddToGroups(adUser.UserId, role);
            return adUser.Username;
        }

        /// <summary>
        /// This will check Azure AD for a user with the alternate email set to a given email address.
        /// </summary>
        /// <param name="recoveryEmail"></param>
        /// <returns>The username</returns>
        public virtual async Task<string> GetUsernameForUserWithRecoveryEmail(string recoveryEmail)
        {
            var filter = $"otherMails/any(c:c eq '{recoveryEmail}')";
            return (await _userAccountService.GetUserByFilter(filter))?.UserPrincipalName;
        }

        /// <summary>
        /// Determine the next available username for a participant based on username format [firstname].[lastname]
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns>next available user principal name</returns>
        public virtual async Task<string> CheckForNextAvailableUsername(string firstName, string lastName)
        {
            var baseUsername = $"{firstName}.{lastName}".ToLower();
            var userFilter = $@"startswith(userPrincipalName,'{baseUsername}')";
            var users = (await _userAccountService.QueryUsers(userFilter)).ToList();
            var domain = "@hearings.reform.hmcts.net";
            if (!users.Any())
            {
                var userPrincipalName = $"{baseUsername}{domain}";
                return userPrincipalName;
            }
            users = users.OrderBy(x => x.UserPrincipalName).ToList();
            var lastUserPrincipalName = users.Last().UserPrincipalName;

            lastUserPrincipalName = GetStringWithoutWord(lastUserPrincipalName, domain);
            lastUserPrincipalName = GetStringWithoutWord(lastUserPrincipalName, baseUsername);


            int.TryParse(lastUserPrincipalName, out var lastNumber);
            lastNumber = 1;

            return $"{baseUsername}{lastNumber}{domain}";
        }

        private string GetStringWithoutWord(string currentWord, string wordToRemove)
        {
            return currentWord.Remove(currentWord.IndexOf(wordToRemove, StringComparison.InvariantCultureIgnoreCase),
                wordToRemove.Length);
        }

        public virtual IEnumerable<string> GetGroupsForRole(string role)
        {
            var roles = new List<string>();
            if (role == "Citizen")
                roles.AddRange(new List<string> {"External", "Participant"});
            else if (role == "Professional")
                roles.AddRange(new List<string> {"External", "VirtualRoomProfessionalUser", "Participant"});
            else if (role == "Judge")
                roles.AddRange(new List<string> {"Internal", "VirtualRoomJudge", "Judge"});
            else if (role == "Administrator")
                roles.AddRange(new List<string> {"Internal", "VirtualRoomHearingAdministrator", "Admin"});
            else if (role == "Clerk") roles.AddRange(new List<string> {"Internal", "VirtualRoomClerk"});

            return roles;
        }

        /// <summary>
        /// Will check the groups for a role. Will then add a user to an AD group if they are not already in it.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        public virtual async Task AddToGroups(string userId, string role)
        {
            var groups = GetGroupsForRole(role);
            var existingGroups = await _userAccountService.GetGroupsForUser(userId);
            foreach (var adGroup in groups)
            {
                if (existingGroups.All(g => g.DisplayName != adGroup))
                {
                    await AddToGroupsByUserId(userId, adGroup);
                }
            }
        }

        public virtual async Task AddToGroupsByUsername(string username, string role)
        {
            var user = await _userAccountService.GetUserById(username);
            await AddToGroups(user.Id, role);
        }

        private async Task AddToGroupsByUserId(string userId, string groupName)
        {
            var group = await _userAccountService.GetGroupByName(groupName);
            if (group == null)
            {
                throw new UserServiceException($"Group {groupName} does not exist", "Invalid group name");
            }
            await _userAccountService.AddUserToGroup(new User {Id = userId}, @group);
        }
    }
}