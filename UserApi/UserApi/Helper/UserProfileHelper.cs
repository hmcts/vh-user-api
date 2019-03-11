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

        public async Task<UserProfile> GetUserProfile(string filter)
        {
            var userRole = string.Empty;
            var userCaseType = new List<string>();
            var user = await _userAccountService.GetUserByFilter(filter);

            if (user == null) return null;

            var userGroups = await _userAccountService.GetGroupsForUser(user.Id);
            if (userGroups != null)
            {
                var userGroupIds = new List<int>();

                foreach (var usrGrp in userGroups)
                {
                    EnumExtensions.TryParse<AadGroup>(usrGrp.DisplayName, out var rtnEnum);

                    if (rtnEnum != null)
                        userGroupIds.Add((int)Enum.Parse(typeof(AadGroup), rtnEnum.ToString()));
                }

                var lstVirtualRoomProfessionalPlusExternal = new List<int>
                    {(int) AadGroup.VirtualRoomProfessional, (int) AadGroup.External};
                var lstMoneyClaimsPlusFinancialRemedy = new List<int>
                    {(int) AadGroup.MoneyClaims, (int) AadGroup.FinancialRemedy};

                foreach (var userGroupId in userGroupIds)
                {
                    switch (userGroupId)
                    {
                        case 1:
                            userRole = UserRole.VhOfficer.ToString();
                            break;
                        case 2:
                            userRole = UserRole.Individual.ToString();
                            break;
                        case 3:
                            userRole = UserRole.Judge.ToString();
                            break;
                        case 4:
                            userRole = UserRole.CaseAdmin.ToString();
                            userCaseType.Add(CaseType.MoneyClaims.ToString());
                            break;
                        case 5:
                            userRole = UserRole.CaseAdmin.ToString();
                            userCaseType.Add(CaseType.FinancialRemedy.ToString());
                            break;
                    }
                }

                if (userGroupIds.All(lstVirtualRoomProfessionalPlusExternal.Contains))
                    userRole = UserRole.Representative.ToString();

                if (userGroupIds.All(lstMoneyClaimsPlusFinancialRemedy.Contains))
                {
                    userRole = UserRole.CaseAdmin.ToString();
                    userCaseType.Add(CaseType.MoneyClaims.ToString());
                    userCaseType.Add(CaseType.FinancialRemedy.ToString());
                }
            }

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
    }
}