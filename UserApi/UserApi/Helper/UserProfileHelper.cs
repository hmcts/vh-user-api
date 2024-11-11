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
    public class UserProfileHelper(IUserAccountService userAccountService, Settings settings)
    {
        private readonly Settings _settings = settings;

        public async Task<UserProfile> GetUserProfileAsync(string filter)
        {
            var user = await userAccountService.GetUserByFilterAsync(filter);

            if (user == null)
            {
                return null;
            }

            var isUserAdmin = await userAccountService.IsUserAdminAsync(user.Id);

            var groups = (await userAccountService.GetGroupsForUserAsync(user.Id))
                .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
                .ToList();

            var userRole = GetUserRole(groups).ToString();

            return GraphUserMapper.MapToUserProfile(user, userRole, isUserAdmin);
        }

        private static UserRole GetUserRole(ICollection<Group> userGroups)
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

        private static bool IsVirtualRoomAdministrator(Group group)
        {
            return string.Equals(nameof(_settings.AdGroup.VirtualRoomAdministrator), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsStaffMember(Group group)
        {
            return string.Equals(nameof(_settings.AdGroup.StaffMember), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsVirtualRoomJudge(Group group)
        {
            return string.Equals(nameof(_settings.AdGroup.VirtualRoomJudge), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsVirtualRoomProfessionalUser(Group group)
        {
            return string.Equals(nameof(_settings.AdGroup.VirtualRoomProfessionalUser), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsExternal(Group group)
        {
            return string.Equals(nameof(_settings.AdGroup.External), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsJudicialOfficeHolder(Group group)
        {
            return string.Equals(nameof(_settings.AdGroup.JudicialOfficeHolder), group.DisplayName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
