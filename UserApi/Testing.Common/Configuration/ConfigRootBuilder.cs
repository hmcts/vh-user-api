using Microsoft.Extensions.Configuration;

namespace Testing.Common.Configuration
{
    public static class ConfigRootBuilder
    {
        private const string UserSecretId = "CF55F1BB-0EE3-456A-A566-70E56AC24C95";
        private const string TestUserSecretId = "de27c4e5-a750-4ee5-af7e-592e4ee78ab2";
        public static IConfigurationRoot Build(string userSecretId = UserSecretId, bool useSecrets = true, bool inlcudeTestUserSecrets = false)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true)
                .AddJsonFile("appsettings.Production.json", true); // CI write variables in the pipeline to this file

            if (useSecrets)
            {
                builder = builder.AddUserSecrets(userSecretId);
            }

            if (inlcudeTestUserSecrets)
            {
                builder.AddUserSecrets(TestUserSecretId);
            }

            return builder.AddEnvironmentVariables()
                .Build();
        }
    }
}
