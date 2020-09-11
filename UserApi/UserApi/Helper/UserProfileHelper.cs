using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserApi.Services;
using UserApi.Services.Models;

namespace UserApi.Helper
{
    public class UserProfileHelper
    {
        private readonly IUserAccountService _userAccountService;

        public UserProfileHelper(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        public async Task<UserProfile> GetUserProfileAsync(string filter)
        {
            var user = await _userAccountService.GetUserByFilterAsync(filter);

            if (user == null)
            {
                return null;
            }

            var groups = (await _userAccountService.GetGroupsForUserAsync(user.Id))
                .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
                .ToList();

            var userRole = GetUserRole(groups).ToString();
            var caseTypes = groups.Where(IsCaseType).Select(x => x.DisplayName).ToList();

            var response = new UserProfile
            {
                UserId = user.Id,
                UserName = user.UserPrincipalName,
                Email = user.Mail,
                DisplayName = user.DisplayName,
                FirstName = user.GivenName,
                LastName = user.Surname,
                UserRole = userRole,
                CaseType = caseTypes
            };

            return response;
        }

        public async Task<UserProfile> GetUserByUserPrincipalAsync(string userPrincipalName)
        {
            var user = await _userAccountService.GetUserByUserPrincipalNameAsync(userPrincipalName);

            if (user == null)
            {
                return null;
            }

            return new UserProfile
            {
                UserId = user.Id,
                UserName = user.UserPrincipalName,
                Email = user.Mail,
                DisplayName = user.DisplayName,
                FirstName = user.GivenName,
                LastName = user.Surname,
                UserRole = null,
                CaseType = null
            };
        }

        private static UserRole GetUserRole(ICollection<Group> userGroups)
        {
            if (userGroups.Any(IsVirtualRoomAdministrator))
            {
                return UserRole.VhOfficer;
            }

            if (userGroups.Any(IsCaseType))
            {
                return UserRole.CaseAdmin;
            }

            if (userGroups.Any(IsVirtualRoomJudge))
            {
                return UserRole.Judge;
            }

            if (userGroups.Any(IsVirtualRoomProfessionalUser))
            {
                return UserRole.Representative;
            }

            if (userGroups.Any(IsExternal))
            {
                return UserRole.Individual;
            }

            throw new UnauthorizedAccessException("Matching user is not registered with valid groups");
        }

        private static bool IsCaseType(Group group)
        {
            return !string.IsNullOrWhiteSpace(group.Description) &&
                    string.Equals("CaseType", group.Description, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsVirtualRoomAdministrator(Group group)
        {
            return string.Equals("VirtualRoomAdministrator", group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsVirtualRoomJudge(Group group)
        {
            return string.Equals("VirtualRoomJudge", group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsVirtualRoomProfessionalUser(Group group)
        {
            return string.Equals("VirtualRoomProfessionalUser", group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsExternal(Group group)
        {
            return string.Equals("External", group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
