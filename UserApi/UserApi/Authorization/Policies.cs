namespace UserApi.Authorization
{
    public static class Policies
    {
        public const string Default = "default";
        public const string ReadProfile = "Profile/Read";
        public const string ReadUsers = "Users/Read/Basic";
        public const string WriteUsers = "Users/Write";
        public const string ReadGroups = "Groups/Read";
        public const string WriteGroups = "Groups/Write";
    }
}