using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetGroupByNameAsyncTests: UserAccountServiceTests
    {
        private const string GroupName = "testGroup";

        [Test]
        public async Task Should_get_group_by_given_name()
        {
            var expectedGroup = new Group{ Id = GroupName, DisplayName = GroupName};
            DistributedCache.Setup(x => x.GetOrAddAsync($"cachekey.ad.group.{GroupName}", It.IsAny<Func<Task<Group>>>()))
                .Callback( (string key, Func<Task<Group>> factory) =>  factory())
                .ReturnsAsync(expectedGroup);
            
            var response = await Service.GetGroupByNameAsync(GroupName);

            response.Should().NotBeNull();
            response.Id.Should().Be(expectedGroup.Id);
            response.DisplayName.Should().Be(expectedGroup.DisplayName);
        }

        [Test]
        public void Should_return_user_exception_for_other_responses()
        {
            var reason = "User not authorised";

            var expectedGroup = new Group {Id = GroupName, DisplayName = GroupName};
            DistributedCache.Setup(x => x.GetOrAddAsync($"cachekey.ad.group.{GroupName}", It.IsAny<Func<Task<Group>>>()))
                .Callback((string key, Func<Task<Group>> factory) => factory())
                .ThrowsAsync(new UserServiceException($"Failed to get group by name {GroupName}", reason));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupByNameAsync(GroupName));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get group by name {GroupName}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
