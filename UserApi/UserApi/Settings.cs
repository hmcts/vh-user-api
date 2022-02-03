namespace UserApi
{
    public class Settings
    {
        public bool DisableHttpsRedirection { get; set; }
        public string DefaultPassword { get; set; }
        public string TestDefaultPassword { get; set; }
        /// <summary>
        ///     Flag to determine if we are running in production environment,
        ///     display the judges list based on the environment
        /// </summary>
        public bool IsLive { get; set; }
        public string ReformEmail { get; set; }
        public AdGroup AdGroup { get; set; }
    }

    public class AdGroup
    {
        public string CaseType { get; set; }
        public string Administrator { get; set; }
        public string Judge { get; set; }
        public string StaffMember { get; set; }
        public string JudgesTestGroup { get; set; }
        public string ProfessionalUser { get; set; }
        public string External { get; set; }
        public string JudicialOfficeHolder { get; set; }
    }
}