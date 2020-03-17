using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetGroupByNameAsyncTests: UserAccountServiceTests
    {
        private const string GroupName = "testGroup";

        [Test]
        public async Task Should_get_group_by_given_name()
        {
            var accessUri = $"{_graphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq '{GroupName}'";
            var graphQueryResponse = new GraphQueryResponse() { Value = new List<Microsoft.Graph.Group> { new Microsoft.Graph.Group()} };

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(graphQueryResponse,HttpStatusCode.OK));

            var response = await _service.GetGroupByNameAsync(GroupName);

            response.Should().NotBeNull();
        }

        [Test]
        public async Task Should_return_user_exception_for_other_responses()
        {
            var reason = "User not authorised";

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.GetGroupByNameAsync(GroupName));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get group by name {GroupName}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
