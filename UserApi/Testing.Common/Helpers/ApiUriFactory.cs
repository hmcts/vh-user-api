namespace Testing.Common.Helpers
{
    public class ApiUriFactory
    {
        public HearingEndpoints HearingEndpoints { get; set; }
        public ReferenceEndpoints ReferenceEndpoints { get; set; }
        public UserEndpoints UserEndpoints { get; set; }
        public AccountEndpoints AccountEndpoints { get; set; }

        public ApiUriFactory()
        {
            HearingEndpoints = new HearingEndpoints();
            ReferenceEndpoints = new ReferenceEndpoints();
            UserEndpoints = new UserEndpoints();
            AccountEndpoints = new AccountEndpoints();
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

    public class AccountEndpoints
    {
        public string AddUserToGroup => $"{ApiRoot}/user/group";
        private string ApiRoot => "accounts";
        public string GetGroupByName(string groupName) => $"{ApiRoot}/group/?name={groupName}";
        public string GetGroupById(string groupId) => $"{ApiRoot}/group/{groupId}";
        public string GetGroupsForUser(string userId) => $"{ApiRoot}/user/{userId}/groups";
    }

    public class UserEndpoints
    {
        private string ApiRoot => "users";
        public string CreateUser => $"{ApiRoot}";
        public string GetUserByAdUserId(string userId) => $"{ApiRoot}/{userId}";
        public string GetUserByAdUserName(string userName) => $"{ApiRoot}/UserName/{userName}";
        public string GetUserByEmail(string email) => $"{ApiRoot}/Email/{email}";
    }
}