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
            var user = await _userAccountService.GetUserByFilter(filter);

            if (user == null)
            {
                return null;
            }

            var userGroups = await _userAccountService.GetGroupsForUser(user.Id);
            var userGroupIds = new List<int>();
            foreach (var usrGrp in userGroups)
            {
                userGroupIds.Add((int) Enum.Parse(typeof(AadGroup), usrGrp.DisplayName));
            }

            var userRole = string.Empty;
            var userCaseType = string.Empty;
            var lstVirtualRoomProfessionalPlusExternal = new List<int> { (int)AadGroup.VirtualRoomAdministrator, (int)AadGroup .External};
            var lstMoneyClaimsPlusFinancialRemedy = new List<int> { (int)AadGroup.MoneyClaims, (int)AadGroup.FinancialRemedy};

            if (userGroupIds.Count == 1)
            {
                switch (userGroupIds[0])
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
                        userCaseType = CaseType.MoneyClaims.ToString();
                        break;
                    case 5:
                        userRole = UserRole.CaseAdmin.ToString();
                        userCaseType = CaseType.FinancialRemedy.ToString();
                        break;
                }
            }

            if (userGroupIds.Any(ug => lstVirtualRoomProfessionalPlusExternal.Contains(ug)))
            {
                userRole = UserRole.Representative.ToString();
            }

            if (userGroupIds.Any(ug => lstMoneyClaimsPlusFinancialRemedy.Contains(ug)))
            {
                userRole = UserRole.CaseAdmin.ToString();
                userCaseType = CaseType.MoneyClaimsPlusFinancialRemedy.ToString();
            }

            var response = new UserProfile
            {
                UserName = user.UserPrincipalName,
                Email = user.Mail,
                FirstName = user.GivenName,
                LastName = user.Surname,
                UserRole = userRole,
                CaseType = userCaseType
            };

            return response;
        }
    }
}