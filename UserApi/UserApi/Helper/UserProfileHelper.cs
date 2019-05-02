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
                UserRole = userRole.ToString(),
                CaseType = userCaseType
            };

            return response;
        }

        private async Task<UserRole> GetUserRole(string userId)
        {
            var userGroupDetails = await _userAccountService.GetGroupsForUser(userId);
            var userGroups = GetUserGroups(userGroupDetails).ToList();

            if (userGroups.Contains(AadGroup.VirtualRoomAdministrator))
            {
                return UserRole.VhOfficer;
            }

            if (userGroups.Contains(AadGroup.MoneyClaims) || userGroups.Contains(AadGroup.FinancialRemedy))
            {
                return UserRole.CaseAdmin;
            }

            if (userGroups.Contains(AadGroup.VirtualRoomJudge))
            {
                return UserRole.Judge;
            }

            if (userGroups.Contains(AadGroup.VirtualRoomProfessionalUser))
            {
                return UserRole.Representative;
            }

            if (userGroups.Contains(AadGroup.External))
            {
                return UserRole.Individual;
            }

            throw new UnauthorizedAccessException("Matching user is not registered with valid groups");
        }

        private static IEnumerable<AadGroup> GetUserGroups(IEnumerable<Group> userGroups)
        {
            foreach (var displayName in userGroups.Select(g => g.DisplayName).Where(g => !string.IsNullOrEmpty(g)))
            {
                if (Enum.TryParse(displayName, out AadGroup @group))
                {
                    yield return group;
                }
            }
        }
    }
}