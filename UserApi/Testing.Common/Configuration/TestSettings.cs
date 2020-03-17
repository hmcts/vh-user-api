using System.Collections.Generic;

namespace Testing.Common.Configuration
{
    public class TestSettings
    {
        public string ExistingUserId { get; set; }
        public string ExistingUserPrincipal { get; set; }
        public string ExistingEmail { get; set; }
        public string ExistingUserFirstname { get; set; }
        public string ExistingUserLastname { get; set; }
        public List<Group> ExistingGroups { get; set; }
        public List<Group> NewGroups { get; set; }
    }
}
