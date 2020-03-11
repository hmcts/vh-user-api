using System.Collections.Generic;
using Testing.Common;

namespace UserApi.AcceptanceTests.Configuration
{
    public class UserApiTestConfig
    {
        public string ExistingEmail { get; set; }
        public List<Group> ExistingGroups { get; set; }
        public string ExistingUserFirstname { get; set; }
        public string ExistingUserLastname { get; set; }
        public string ExistingUserId { get; set; }
        public string ExistingUserPrincipal { get; set; }
        public List<Group> NewGroups { get; set; }
        public string ReformEmail { get; set; }
    }
}
