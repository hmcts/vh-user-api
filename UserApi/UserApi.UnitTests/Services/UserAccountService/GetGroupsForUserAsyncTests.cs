using System.Collections.Generic;
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
    public class GetGroupsForUserAsyncTests: UserAccountServiceTests
    {
        private const string UserId = "userId";
        private string accessUri => $"{GraphApiSettings.GraphApiBaseUri}v1.0/users/{UserId}/memberOf";

        [Test]
        public async Task Should_get_group_by_given_id()
        {
            var directoryObject = new DirectoryObject() { AdditionalData = new Dictionary<string, object> ()};
            const string json = @"[ 
                                { ""@odata.type"" : ""#microsoft.graph.group"" },
                                { ""@odata.type"" : ""#microsoft.graph.group"" },
                                { ""@odata.type"" : ""#microsoft.graph.test"" }
                            ]";
            
            directoryObject.AdditionalData.Add("value", json);

            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(directoryObject, HttpStatusCode.OK));

            var response = await Service.GetGroupsForUserAsync(UserId);

            response.Should().NotBeNull();
            response.Count.Should().Be(2);
            SecureHttpRequest.Verify(s => s.GetAsync(GraphApiSettings.AccessToken, accessUri), Times.Once);
        }

        [Test]
        public async Task Should_return_empty_when_no_matching_group_by_given_userid()
        { 
            SecureHttpRequest.Setup(s => s.GetAsync(GraphApiSettings.AccessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("Not found", HttpStatusCode.NotFound));

            var response = await Service.GetGroupsForUserAsync(UserId);

            response.Should().BeEmpty();
        }

        [Test]
        public void Should_return_user_exception_for_other_responses()
        {
            const string reason = "User not authorised";

            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupsForUserAsync(UserId));

            response.Should().NotBeNull();
            response!.Message.Should().Be($"Failed to get group for user {UserId}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
