using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using UserApi.Helper;

namespace UserApi.UnitTests.Helpers
{
    public class UsernameGeneratorTests
    {
        private IEnumerable<string> _existingUsernames;
        private const string Domain = "hearings.test.server.net";
        private const string UsernameBase = "existing.user";

        [Test]
        public void Should_throw_if_given_bad_arguments()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => UsernameGenerator.GetIncrementedUsername(null, "domain", _existingUsernames));
            exception.ParamName.Should().Be("usernameBase");

            exception = Assert.Throws<ArgumentNullException>(() => UsernameGenerator.GetIncrementedUsername("username", null, _existingUsernames));
            exception.ParamName.Should().Be("domain");
        }

        [Test]
        public void Should_increment_username_even_past_two_digits()
        {
            // given there are several existing users, not necessarily in order
            var suffixes = new[] {"", "1", "2", "10", "5", "6", "4", "3", "7", "9", "8"};
            var existingUsers = suffixes.Select(s => UsernameBase + s).ToArray();
            GivenApiReturnsExistingUsers(existingUsers);

            var nextAvailable = UsernameGenerator.GetIncrementedUsername(UsernameBase, Domain, _existingUsernames);
            nextAvailable.Should().Be("existing.user11@" + Domain);
        }

        [Test]
        public void Should_generate_the_first_available_username()
        {
            // given there already exists a number of users but there's a gap in the sequence
            const string username = "existing.user";
            GivenApiReturnsExistingUsers(username, username + "1", username + "3");

            var nextAvailable = UsernameGenerator.GetIncrementedUsername(UsernameBase, Domain, _existingUsernames);
            nextAvailable.Should().Be("existing.user2@" + Domain);
        }

        [Test]
        public void Should_ignore_partially_matching_usernames_when_generating_a_new_username()
        {
            // given there are some users already with partially matching usernames
            GivenApiReturnsExistingUsers("existing.user", "existing.username1", "existing.username2", "existing.user1");

            var nextAvailable = UsernameGenerator.GetIncrementedUsername(UsernameBase, Domain, _existingUsernames);
            nextAvailable.Should().Be("existing.user2@" + Domain);
        }

        [Test]
        public void Should_ignore_case_when_checking_next_username()
        {
            // given we have users matching the username but with differing format,
            // now, this shouldn't naturally occur but in case someone adds a user manually we need to handle it gracefully
            GivenApiReturnsExistingUsers("EXisting.User", "ExistIng.UseR1");

            var nextAvailable = UsernameGenerator.GetIncrementedUsername(UsernameBase, Domain, _existingUsernames);
            nextAvailable.Should().Be("existing.user2@" + Domain);
        }

        private void GivenApiReturnsExistingUsers(params string[] existingUsernames)
        {
            _existingUsernames = existingUsernames.Select(username => username + "@" + Domain);
        }
    }
}
