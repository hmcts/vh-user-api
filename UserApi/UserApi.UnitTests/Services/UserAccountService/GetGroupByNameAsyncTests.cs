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
        private string groupName = "testGroup";

        [Test]
        public async Task Should_get_group_by_given_name()
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq '{groupName}'";
            var graphQueryResponse = new GraphQueryResponse() { Value = new List<Microsoft.Graph.Group> { new Microsoft.Graph.Group()} };

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse,HttpStatusCode.OK));

            var response = await _service.GetGroupByNameAsync(groupName);

            response.Should().NotBeNull();
        }

        [Test]
        public async Task Should_return_user_exception_for_other_responses()
        {
            var reason = "User not authorised";

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.GetGroupByNameAsync(groupName));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get group by name {groupName}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
