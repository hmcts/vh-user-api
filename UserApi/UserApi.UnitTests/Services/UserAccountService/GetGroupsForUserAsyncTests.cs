using FluentAssertions;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Testing.Common.Helpers;
using UserApi.Security;

namespace UserApi.UnitTests.Services
{
    public class GetGroupsForUserAsyncTests: UserAccountServiceTests
    {
        private string userId = "userId";
        private string accessUri => $"{_graphApiSettings.GraphApiBaseUri}v1.0/users/{userId}/memberOf";

        [Test]
        public async Task Should_get_group_by_given_id()
        { 
            var group = new Group();
            var directoryObject = new DirectoryObject() { AdditionalData = new Dictionary<string, object> ()};
            string json = @"[ 
                                { ""@odata.type"" : ""#microsoft.graph.group"" },
                                { ""@odata.type"" : ""#microsoft.graph.group"" },
                                { ""@odata.type"" : ""#microsoft.graph.test"" }
                            ]";
            
            directoryObject.AdditionalData.Add("value", json);

            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            var response = await _service.GetGroupsForUserAsync(userId);

            response.Should().NotBeNull();
            response.Count.Should().Be(2);
            _secureHttpRequest.Verify(s => s.GetAsync(_graphApiSettings.AccessToken, accessUri), Times.Once);
        }

        [Test]
        public async Task Should_return_empty_when_no_matching_group_by_given_userid()
        { 
            _secureHttpRequest.Setup(s => s.GetAsync(_graphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("Not found", HttpStatusCode.NotFound));

            var response = await _service.GetGroupsForUserAsync(userId);

            response.Should().BeEmpty();
        }

        [Test]
        public async Task Should_return_user_exception_for_other_responses()
        {
            var reason = "User not authorised";

            _secureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await _service.GetGroupsForUserAsync(userId));

            response.Should().NotBeNull();
            response.Message.Should().Be($"Failed to get group for user {userId}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
