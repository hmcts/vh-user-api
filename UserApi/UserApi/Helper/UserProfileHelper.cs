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

        /// <summary>
        /// Mappings for AD groups since the display names can contain spaces
        /// </summary>
        private static readonly Dictionary<string, AdGroup> GroupMappings = new Dictionary<string, AdGroup>
        {
            {"External", AdGroup.External},
            {"VirtualRoomAdministrator", AdGroup.VirtualRoomAdministrator},
            {"VirtualRoomJudge", AdGroup.VirtualRoomJudge},
            {"VirtualRoomProfessionalUser", AdGroup.VirtualRoomProfessionalUser},
            {"Financial Remedy", AdGroup.FinancialRemedy},
            {"Civil Money Claims", AdGroup.MoneyClaims}
        };

        private static readonly Dictionary<AdGroup, string> CaseTypeMappings = new Dictionary<AdGroup, string>
        {
            { AdGroup.MoneyClaims, "Civil Money Claims" },
            { AdGroup.FinancialRemedy, "Financial Remedy" }
        };

        public UserProfileHelper(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        public async Task<UserProfile> GetUserProfileAsync(string filter)
        {
            var user = await _userAccountService.GetUserByFilter(filter);

            if (user == null)
            {
                return null;
            }

            var userGroupDetails = await _userAccountService.GetGroupsForUser(user.Id);
            var userGroups = GetUserGroups(userGroupDetails).ToList();
            
            var userRole = GetUserRole(userGroups).ToString();
            var caseTypes = GetUserCaseTypes(userGroups).ToList();

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

        private static bool IsCaseType(AdGroup group) => CaseTypeMappings.ContainsKey(group);

        private static IEnumerable<string> GetUserCaseTypes(IEnumerable<AdGroup> userGroups)
        {
            return userGroups
                .Where(IsCaseType)
                .Select(c => CaseTypeMappings[c]);
        }

        private static UserRole GetUserRole(ICollection<AdGroup> userGroups)
        {
            if (userGroups.Contains(AdGroup.VirtualRoomAdministrator))
            {
                return UserRole.VhOfficer;
            }

            if (userGroups.Any(IsCaseType))
            {
                return UserRole.CaseAdmin;
            }

            if (userGroups.Contains(AdGroup.VirtualRoomJudge))
            {
                return UserRole.Judge;
            }

            if (userGroups.Contains(AdGroup.VirtualRoomProfessionalUser))
            {
                return UserRole.Representative;
            }

            if (userGroups.Contains(AdGroup.External))
            {
                return UserRole.Individual;
            }

            throw new UnauthorizedAccessException("Matching user is not registered with valid groups");
        }

        private static IEnumerable<AdGroup> GetUserGroups(IEnumerable<Group> userGroups)
        {
            foreach (var displayName in userGroups.Select(g => g.DisplayName).Where(g => !string.IsNullOrEmpty(g)))
            {
                if (GroupMappings.TryGetValue(displayName, out var adGroup))
                {
                    yield return adGroup;
                }
            }
        }
    }
}
