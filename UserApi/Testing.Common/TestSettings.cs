using System.Collections.Generic;

namespace Testing.Common
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
        public string Individual { get; set; }
        public string ReformEmail { get; set; }
        public string Representative { get; set; }
        public string VhOfficer { get; set; }
        public string CaseAdmin { get; set; }
        public string Judge { get; set; }
        public string VhOfficerCaseAdmin { get; set; }
    }
}
