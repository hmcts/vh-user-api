namespace Testing.Common.Helpers
{
    public class ApiUriFactory
    {
        public ApiUriFactory()
        {
            UserEndpoints = new UserEndpoints();
            AccountEndpoints = new AccountEndpoints();
        }

        public UserEndpoints UserEndpoints { get; set; }
        public AccountEndpoints AccountEndpoints { get; set; }
    }

    public class AccountEndpoints
    {
        private string ApiRoot => "accounts";
        public string AddUserToGroup => $"{ApiRoot}/user/group";

        public string GetGroupByName(string groupName)
        {
            return $"{ApiRoot}/group/?name={groupName}";
        }

        public string GetGroupById(string groupId)
        {
            return $"{ApiRoot}/group/{groupId}";
        }

        public string GetGroupsForUser(string userId)
        {
            return $"{ApiRoot}/user/{userId}/groups";
        }
    }

    public class UserEndpoints
    {
        private string ApiRoot => "users";
        public string CreateUser => $"{ApiRoot}";

        public string GetUserByAdUserId(string userId)
        {
            return $"{ApiRoot}/{userId}";
        }

        public string GetUserByAdUserName(string userName)
        {
            return $"{ApiRoot}/username/{userName}";
        }

        public string GetUserByEmail(string email)
        {
            return $"{ApiRoot}/email/{email}";
        }
    }
}