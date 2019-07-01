using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Testing.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;

namespace UserApi.UnitTests.Services
{
    public class UserAccountServiceTests
    {
        const string domain = "@hearings.reform.hmcts.net";
        private Mock<SecureHttpRequest> _secureHttpRequest;
        private GraphApiSettings _graphApiSettings;
        private Mock<IIdentityServiceApiClient> _identityServiceApiClient;
        private UserAccountService _service;

        [SetUp]
        public void Setup()
        {
            _secureHttpRequest = new Mock<SecureHttpRequest>();

            var settings = TestConfig.Instance.Settings;
            var tokenProvider = new TokenProvider(TestConfig.Instance.AzureAd);
            _graphApiSettings = new GraphApiSettings(tokenProvider, TestConfig.Instance.AzureAd);
            _identityServiceApiClient = new Mock<IIdentityServiceApiClient>();
            _service = new UserAccountService(_secureHttpRequest.Object, _graphApiSettings, _identityServiceApiClient.Object, settings);
        }

        [Test]
        public async Task should_increment_username_even_past_two_digits()
        {
            // given there are several existing users, not necessarily in order
            const string username = "existing.user";
            var suffixes = new[] {"", "1", "2", "10", "5", "6", "4", "3", "7", "9", "8"};
            var existingUsers = suffixes.Select(s => username + s + domain).ToList();
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWith("existing.user"))
                .ReturnsAsync(existingUsers);

            Console.WriteLine("With existing users:");
            existingUsers.ForEach(Console.WriteLine);
            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync("Existing", "User");
            Assert.AreEqual("existing.user11" + domain, nextAvailable);
        }

        [Test]
        public async Task should_generate_the_first_available_username()
        {
            // given there already exists a number of users but there's a gap in the sequence
            const string username = "existing.user";
            var existingUsers = new List<string> { username + domain, username + "1" + domain, username + "3" + domain };
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWith("existing.user"))
                .ReturnsAsync(existingUsers);

            Console.WriteLine("With existing users:");
            existingUsers.ForEach(Console.WriteLine);
            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync("Existing", "User");
            Assert.AreEqual("existing.user2" + domain, nextAvailable);
        }

        [Test]
        public async Task should_ignore_partially_matching_usernames_when_generating_a_new_username()
        {
            // given there are some users already with partially matching usernames
            var existingUsers = new List<string>
            {
                "existing.user" + domain,
                "existing.username1" + domain,
                "existing.username2" + domain,
                "existing.user1" + domain
            };
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWith("existing.user"))
                .ReturnsAsync(existingUsers);

            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync("Existing", "User");
            Assert.AreEqual("existing.user2" + domain, nextAvailable);
        }

        [Test]
        public async Task should_ignore_case_when_checking_next_username()
        {
            // given we have users matching the username but with differing format,
            // now, this shouldn't naturally occur but in case someone adds a user manually we need to handle it gracefully
            const string username = "existing.user";
            var existingUsers = new List<string> { "EXisting.User" + domain, "ExistIng.UseR1" + domain };
            _identityServiceApiClient.Setup(x => x.GetUsernamesStartingWith("existing.user"))
                .ReturnsAsync(existingUsers);

            var nextAvailable = await _service.CheckForNextAvailableUsernameAsync("Existing", "User");
            Assert.AreEqual("existing.user2" + domain, nextAvailable);
        }
    }
}
