namespace Testing.Common.Helpers
{
    public class ApiUriFactory
    {
        public HearingEndpoints HearingEndpoints { get; set; }
        public ReferenceEndpoints ReferenceEndpoints { get; set; }
        public UserAccountEndpoints UserAccountEndpoints { get; set; }

        public ApiUriFactory()
        {
            HearingEndpoints = new HearingEndpoints();
            ReferenceEndpoints = new ReferenceEndpoints();
            UserAccountEndpoints = new UserAccountEndpoints();
        }
    }
    
    public class HearingEndpoints
    {
        private string ApiRoot => "api/hearings";
        public string CreateAHearing => ApiRoot;
    }

    public class ReferenceEndpoints
    {
        private string ApiRoot => "api/hearings";
        public string GetHearingTypes => $"{ApiRoot}/types";
        public string GetHearingMediums => $"{ApiRoot}/mediums";
        public string GetHearingStatusTypes => $"{ApiRoot}/statustypes";
        public string GetParticipantRoles => $"{ApiRoot}/participantroles";
        public string GetCourts => $"{ApiRoot}/courts";
    }

    public class UserAccountEndpoints
    {
        private string ApiRoot => "api/accounts";

        public string CreateUser => $"{ApiRoot}/user";
        public string AddUserToGroup => $"{ApiRoot}/user/group";
        public string SetRecoveryEmail => $"{ApiRoot}/user/recovery";
        public string GetUserByAdUserId(string userId) => $"{ApiRoot}/user/{userId}";
        public string GetUserByRecoveryEmail(string recoveryEmail) => $"{ApiRoot}/user?recoveryMail={recoveryEmail}";
        public string GetGroupsForUser(string userId) => $"{ApiRoot}/user/{userId}/groups";
        public string GetGroupByName(string groupName) => $"{ApiRoot}/group/?name={groupName}";
        public string GetGroupById(string groupId) => $"{ApiRoot}/group/{groupId}";
    }
}