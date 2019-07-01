namespace UserApi.Services
{
    public class IncrementingUsername
    {
        private readonly string _usernameBase;
        private readonly string _domain;

        public IncrementingUsername(string usernameBase, string domain)
        {
            _usernameBase = usernameBase;
            _domain = "@" + domain;
        }

        public string WithoutNumberSuffix => _usernameBase + _domain;

        public string WithSuffix(int suffix)
        {
            return _usernameBase + suffix + _domain;
        }
    }
}