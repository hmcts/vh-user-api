using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserApi.Contract.Responses;
using UserApi.Services;

namespace UserApi.Helper
{
    public class UserProfileHelper
    {
        private readonly IUserAccountService _userAccountService;
        private readonly Settings _settings;
        
        public UserProfileHelper(IUserAccountService userAccountService, Settings settings)
        {
            _userAccountService = userAccountService;
            _settings = settings;
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

        private UserRole GetUserRole(ICollection<Group> userGroups)
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
            
            if (userGroups.Any(IsJudicialOfficeHolder))
            {
                return UserRole.JudicialOfficeHolder;
            }

            if (userGroups.Any(IsExternal))
            {
                return UserRole.Individual;
            }
            
            if (userGroups.Any(IsStaffMember))
            {
                return UserRole.StaffMember;
            }

            return UserRole.None;
        }

        private bool IsCaseType(Group group)
        {
            return !string.IsNullOrWhiteSpace(group.Description) &&
                    string.Equals(AdGroup.CaseType, group.Description, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsVirtualRoomAdministrator(Group group)
        {
            return string.Equals(AdGroup.Administrator, group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsVirtualRoomJudge(Group group)
        {
            return string.Equals(AdGroup.Judge, group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsVirtualRoomProfessionalUser(Group group)
        {
            return string.Equals(AdGroup.ProfessionalUser, group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsExternal(Group group)
        {
            return string.Equals(AdGroup.External, group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
        
        private bool IsJudicialOfficeHolder(Group group)
        {
            return string.Equals(AdGroup.JudicialOfficeHolder, group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
        private bool IsStaffMember(Group group)
        {
            return string.Equals(AdGroup.StaffMember, group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
