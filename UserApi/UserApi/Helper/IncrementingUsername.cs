using System;
using System.Collections.Generic;
using System.Linq;

namespace UserApi.Helper;

public static class UsernameGenerator
{
    public static string GetIncrementedUsername(string usernameBase, string domain, IEnumerable<string> existingUsernames)
    {
        if (string.IsNullOrEmpty(usernameBase))
            throw new ArgumentNullException(nameof(usernameBase));
        if (string.IsNullOrEmpty(domain))
            throw new ArgumentNullException(nameof(domain));

        var baseLower = usernameBase.ToLowerInvariant();
        var fullBase = baseLower + "@" + domain;
        var users = new HashSet<string>(existingUsernames.Select(u => u.ToLowerInvariant()));

        if (!users.Contains(fullBase) )
            return fullBase;

        int suffix = 1;
        string withSuffix;
        do
        {
            withSuffix = baseLower + suffix + "@" + domain;
            suffix++;
        }
        while (users.Contains(withSuffix));

        return withSuffix;
    }
}
