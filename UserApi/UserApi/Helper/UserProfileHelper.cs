using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserApi.Contract.Responses;
using UserApi.Mappers;
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

            var isUserAdmin = await _userAccountService.IsUserAdminAsync(user.Id);

            var groups = (await _userAccountService.GetGroupsForUserAsync(user.Id))
                .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
                .ToList();

            var userRole = GetUserRole(groups).ToString();

            return GraphUserMapper.MapToUserProfile(user, userRole, isUserAdmin);
        }

        private UserRole GetUserRole(ICollection<Group> userGroups)
        {
            if (userGroups.Any(IsVirtualRoomAdministrator))
            {
                return UserRole.VhOfficer;
            }

            if (userGroups.Any(IsStaffMember))
            {
                return UserRole.StaffMember;
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

            return UserRole.None;
        }

        private bool IsVirtualRoomAdministrator(Group group)
        {
            return string.Equals(nameof(_settings.GroupId.VirtualRoomAdministrator), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsStaffMember(Group group)
        {
            return string.Equals(nameof(_settings.GroupId.StaffMember), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsVirtualRoomJudge(Group group)
        {
            return string.Equals(nameof(_settings.GroupId.VirtualRoomJudge), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsVirtualRoomProfessionalUser(Group group)
        {
            return string.Equals(nameof(_settings.GroupId.VirtualRoomProfessionalUser), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsExternal(Group group)
        {
            return string.Equals(nameof(_settings.GroupId.External), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsJudicialOfficeHolder(Group group)
        {
            return string.Equals(nameof(_settings.GroupId.JudicialOfficeHolder), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
