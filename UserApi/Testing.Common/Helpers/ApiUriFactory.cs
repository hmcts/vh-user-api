using System;

namespace Testing.Common.Helpers
{
    public static class UserApiUriFactory
    {
        public static class UserEndpoints
        {
            private const string ApiRoot = "users";
            public static string CreateUser => $"{ApiRoot}";
            public static string GetUserByAdUserId(string userId) => $"{ApiRoot}/{userId}";
            public static string GetUserByAdUserName(string userName) => $"{ApiRoot}/username/{userName}";
            public static string GetUserByEmail(string email) => $"{ApiRoot}/email/{email}";
            public static string GetJudgesByUsername() => $"{ApiRoot}/judgesbyusername";
            public static string DeleteUser(string userName) => $"{ApiRoot}/username/{userName}";
            public static string ResetUserPassword() => $"{ApiRoot}";
            public static string UpdateUserAccount(Guid userId) => $"{ApiRoot}/username/{userId}";
        }

        public static class HealthCheckEndpoints
        {
            private const string ApiRoot = "healthcheck";
            public static string CheckServiceHealth => $"{ApiRoot}/health";
        }
    }
}