using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services
{
    public class GetGroupByNameAsyncTests: UserAccountServiceTests
    {
        private const string GroupName = "testGroup";

        [Test]
        public async Task Should_get_group_by_given_name()
        {
            var accessUri = $"{GraphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq '{GroupName}'";
            var graphQueryResponse = new GraphQueryResponse() { Value = new List<Microsoft.Graph.Group> { new Microsoft.Graph.Group()} };

            SecureHttpRequest
                .Setup(s => s.GetAsync(GraphApiSettings.AccessToken, accessUri))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse,HttpStatusCode.OK));

            var response = await Service.GetGroupByNameAsync(GroupName);

            response.Should().NotBeNull();
        }

        [Test]
        public void Should_return_user_exception_for_other_responses()
        {
            const string reason = "User not authorised";

            SecureHttpRequest
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupByNameAsync(GroupName));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get group by name {GroupName}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
