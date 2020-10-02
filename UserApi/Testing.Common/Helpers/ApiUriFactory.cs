namespace Testing.Common.Helpers
{
    public static class UserApiUriFactory
    {
        public static class AccountEndpoints
        {
            private const string ApiRoot = "accounts";
            public static string AddUserToGroup = $"{ApiRoot}/user/group";
            public static string GetGroupByName(string groupName) => $"{ApiRoot}/group/?name={groupName}";
            public static string GetGroupById(string groupId) => $"{ApiRoot}/group/{groupId}";
            public static string GetGroupsForUser(string userId) => $"{ApiRoot}/user/{userId}/groups";
        }

        public static class UserEndpoints
        {
            private const string ApiRoot = "users";
            public static string CreateUser => $"{ApiRoot}";
            public static string GetUserByAdUserId(string userId) => $"{ApiRoot}/{userId}";
            public static string GetUserByAdUserName(string userName) => $"{ApiRoot}/username/{userName}";
            public static string GetUserByEmail(string email) => $"{ApiRoot}/email/{email}";
            public static string GetJudges() => $"{ApiRoot}/judges";
            public static string RefreshJudgesCache() => $"{ApiRoot}/judges/cache";
            public static string DeleteUser(string userName) => $"{ApiRoot}/username/{userName}";
            public static string UpdateUser() => $"{ApiRoot}";
        }

        public static class HealthCheckEndpoints
        {
            private const string ApiRoot = "healthcheck";
            public static string CheckServiceHealth => $"{ApiRoot}/health";
        }
    }
}