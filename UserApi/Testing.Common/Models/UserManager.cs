using System.Collections.Generic;
using System.Linq;
using UserApi.Helper;

namespace Testing.Common.Models;

public static class UserManager
{
    public static UserAccount GetUserFromRole(List<UserAccount> userAccounts, UserRole role)
    {
        return userAccounts.First(x => x.Role.ToLower().Equals(role.ToString().ToLower()));
    }
}