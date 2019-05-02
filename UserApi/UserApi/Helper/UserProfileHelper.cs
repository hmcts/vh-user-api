using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
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

        public async Task<UserProfile> GetUserProfile(string filter)
        {
            var userCaseType = new List<string>();
            var user = await _userAccountService.GetUserByFilter(filter);

            if (user == null) return null;

            var userRole = await GetUserRole(user.Id);

            var response = new UserProfile
            {
                UserId = user.Id,
                UserName = user.UserPrincipalName,
                Email = user.Mail,
                DisplayName = user.DisplayName,
                FirstName = user.GivenName,
                LastName = user.Surname,
                UserRole = userRole,
                CaseType = userCaseType
            };

            return response;
        }

        private async Task<string> GetUserRole(string userId)
        {
            var userGroupDetails = await _userAccountService.GetGroupsForUser(userId);
            var userGroups = GetUserGroups(userGroupDetails).ToList();

            if (userGroups.Contains(AadGroup.VirtualRoomAdministrator) && userGroups.Contains(AadGroup.Internal))
            {
                return UserRole.VhOfficer.ToString();
            }

            if (userGroups.Contains(AadGroup.MoneyClaims) || userGroups.Contains(AadGroup.FinancialRemedy))
            {
                return UserRole.CaseAdmin.ToString();
            }

            if (userGroups.Contains(AadGroup.Internal) && userGroups.Contains(AadGroup.VirtualRoomJudge))
            {
                return UserRole.Judge.ToString();
            }

            if (userGroups.Contains(AadGroup.External) && userGroups.Contains(AadGroup.VirtualRoomProfessionalUser))
            {
                return UserRole.Representative.ToString();
            }

            return userGroups.Contains(AadGroup.External) ? UserRole.Individual.ToString() : string.Empty;
        }

        private static IEnumerable<AadGroup> GetUserGroups(IEnumerable<Group> userGroups)
        {
            foreach (var displayName in userGroups.Select(g => g.DisplayName))
            {
                if (Enum.TryParse(displayName, out AadGroup @group))
                {
                    yield return group;
                }
            }
        }
    }
}