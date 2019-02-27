namespace Testing.Common.Helpers
{
    public class ApiUriFactory
    {
        public UserEndpoints UserEndpoints { get; set; }
        public AccountEndpoints AccountEndpoints { get; set; }

        public ApiUriFactory()
        {
            UserEndpoints = new UserEndpoints();
            AccountEndpoints = new AccountEndpoints();
        }
    }

    public class AccountEndpoints
    {
        private string ApiRoot => "accounts";
        public string AddUserToGroup => $"{ApiRoot}/user/group";
        public string GetGroupByName(string groupName) => $"{ApiRoot}/group/?name={groupName}";
        public string GetGroupById(string groupId) => $"{ApiRoot}/group/{groupId}";
        public string GetGroupsForUser(string userId) => $"{ApiRoot}/user/{userId}/groups";
    }

    public class UserEndpoints
    {
        private string ApiRoot => "users";
        public string CreateUser => $"{ApiRoot}";
        public string GetUserByAdUserId(string userId) => $"{ApiRoot}/{userId}";
        public string GetUserByAdUserName(string userName) => $"{ApiRoot}/username/{userName}";
        public string GetUserByEmail(string email) => $"{ApiRoot}/email/{email}";
    }
}