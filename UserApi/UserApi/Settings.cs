using System.Diagnostics.CodeAnalysis;

namespace UserApi
{
    [ExcludeFromCodeCoverage]
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
        public FeatureToggle FeatureToggle { get; set; }
        public AdGroup AdGroup { get; set; }
        public bool ClientStub { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class FeatureToggle
    {
        public string SdkKey { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class AdGroup
    {
        public string External { get; set; }
        public string Internal { get; set; }
        public string VirtualRoomProfessionalUser { get; set; }
        public string JudicialOfficeHolder { get; set; }
        public string VirtualRoomJudge { get; set; }
        public string TestAccount { get; set; }
        public string VirtualRoomAdministrator { get; set; }
        public string StaffMember { get; set; }
        public string UserApiTestGroup { get; set; }
    }
}