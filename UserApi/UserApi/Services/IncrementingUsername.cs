using System;
using System.Collections.Generic;
using System.Linq;

namespace UserApi.Services
{
    public class IncrementingUsername
    {
        private readonly string _usernameBase;
        private readonly string _domain;

        public IncrementingUsername(string usernameBase, string domain)
        {
            if (string.IsNullOrEmpty(usernameBase))
            {
                throw new ArgumentNullException(nameof(usernameBase));
            }

            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            _usernameBase = usernameBase.ToLowerInvariant();
            _domain = "@" + domain;
        }

        public string WithoutNumberSuffix => _usernameBase + _domain;

        public string WithSuffix(int suffix)
        {
            return _usernameBase + suffix + _domain;
        }

        public string GetGivenExistingUsers(IEnumerable<string> existingUsernames)
        {
            var users = new HashSet<string>(existingUsernames.Select(u => u.ToLowerInvariant()));

            if (!users.Contains(WithoutNumberSuffix))
            {
                return WithoutNumberSuffix;
            }

            var suffix = 1;
            while (users.Contains(WithSuffix(suffix)))
            {
                suffix += 1;
            }

            return WithSuffix(suffix);
        }
    }
}
