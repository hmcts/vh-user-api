using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using UserApi.Helper;
using UserApi.Services;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace UserApi.UnitTests.Helpers
{
    public class UserProfileHelperTests
    {
        private Mock<IUserAccountService> _accountService;
        private UserProfileHelper _helper;

        private const string Filter = "some filter";
        protected const string Domain = "@hearings.test.server.net";

        [SetUp]
        public void Setup()
        {
            _accountService = new Mock<IUserAccountService>();
            _helper = new UserProfileHelper(_accountService.Object);
        }
       
        [Test]
        public async Task Should_return_null_for_no_user_found()
        {
            _accountService.Setup(x => x.GetUserByFilterAsync(Filter)).ReturnsAsync((User) null);
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.Should().BeNull();
        }
        
        [Test]
        public async Task Should_return_user_data()
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Mail = "bob@contoso.com",
                DisplayName = "Bob McGregor",
                GivenName = "Bob",
                Surname = "McGregor",
                UserPrincipalName = "bob.mcgregor@hearings.test.server.net"
            };

            GivenFilterReturnsUserWithGroups(user, null, "Ext");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.DisplayName.Should().Be(user.DisplayName);
            userProfile.FirstName.Should().Be(user.GivenName);
            userProfile.LastName.Should().Be(user.Surname);
            userProfile.Email.Should().Be(user.Mail);
            userProfile.UserId.Should().Be(user.Id);
            userProfile.UserName.Should().Be(user.UserPrincipalName);
        }
        
        [Test]
        public async Task Should_return_user_data_when_email_has_quotes()
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Mail = "bo'b@cont'oso.com",
                DisplayName = "Bob McGregor",
                GivenName = "Bob",
                Surname = "McGregor",
                UserPrincipalName = "bo'b.mcg'regor@hearings.test.server.net"
            };

            GivenFilterReturnsUserWithGroups(user, null, "Ext");
            
            var userProfile = await _helper.GetUserProfileAsync(Filter);

            userProfile.DisplayName.Should().Be(user.DisplayName);
            userProfile.FirstName.Should().Be(user.GivenName);
            userProfile.LastName.Should().Be(user.Surname);
            userProfile.Email.Should().Be(user.Mail);
            userProfile.UserId.Should().Be(user.Id);
            userProfile.UserName.Should().Be(user.UserPrincipalName);
        }

        private void GivenFilterReturnsUserWithGroups(User user, string description = null, params string[] groupDisplayNames)
        {
            _accountService.Setup(x => x.GetUserByFilterAsync(Filter)).ReturnsAsync(user);

            var groups = groupDisplayNames.Select(aadGroup => new Group { DisplayName = aadGroup, Description = description }).ToArray();

            _accountService.Setup(x => x.GetGroupsForUserAsync(user.Id)).ReturnsAsync(new List<Group>(groups));
        }
    }
}