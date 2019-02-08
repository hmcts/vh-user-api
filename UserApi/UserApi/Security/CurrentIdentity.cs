using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace UserApi.Security
{
    /// <summary>
    /// Active directory information for current user
    /// </summary>
    public interface IUserIdentity
    {
        IEnumerable<string> GetGroupDisplayNames();

        bool IsAdministratorRole();
    }

    public class UserIdentity : IUserIdentity
    {
        private static readonly string[] AcceptedAdministratorRoles = { "Civil Money Claims", "Tax", "Financial Remedy" };

        private readonly IActiveDirectoryGroup _activeDirectory;
        private readonly ClaimsPrincipal _currentUser;

        public UserIdentity(IActiveDirectoryGroup activeDirectory, ClaimsPrincipal currentUser)
        {
            _activeDirectory = activeDirectory;
            _currentUser = currentUser;
        }

        private IEnumerable<Claim> GetGroups()
        {
            return _currentUser.Claims.Where(x => x.Type == "groups").ToList();
        }

        public IEnumerable<string> GetGroupDisplayNames()
        {
            return GetGroups().Select(x => _activeDirectory.GetGroupDisplayName(x.Value)).ToList();
        }

        public bool IsAdministratorRole()
        {
            var groups = GetGroupDisplayNames().ToList();

            return HasHearingsAdministratorRole(groups) || 
                   HasCaseAdministratorRole(groups);
        }

        private static bool HasCaseAdministratorRole(IEnumerable<string> groups)
        {
            return groups.Any(g => AcceptedAdministratorRoles.Contains(g));
        }

        private bool HasHearingsAdministratorRole(ICollection<string> groups)
        {
            const string internalGroup = "Internal";
            const string administratorGroup = "HearingAdministrator";
            return groups.Contains(internalGroup) && groups.Contains(administratorGroup);
        }
    }
}
