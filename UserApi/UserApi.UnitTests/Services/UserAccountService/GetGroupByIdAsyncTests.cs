using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService
{
    public class GetGroupByIdAsyncTests: UserAccountServiceTests
    {
        private const string GroupId = "testId";

        [Test]
        public async Task Should_get_group_by_given_id()
        {
            var accessUri = $"{GraphApiSettings.GraphApiBaseUri}v1.0/groups/{GroupId}";
            var group = new Microsoft.Graph.Group() { Id = GroupId };

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(group, HttpStatusCode.OK));

            var response = await Service.GetGroupByIdAsync(GroupId);

            response.Should().NotBeNull();
            response.Id.Should().Be(GroupId);
            SecureHttpRequest.Verify(s => s.GetAsync(accessToken, accessUri), Times.Once);
        }

        [Test]
        public async Task Should_return_null_when_no_matching_group_by_given_id()
        {
            var accessUri = $"{GraphApiSettings.GraphApiBaseUri}v1.0/groups/{GroupId}";

            var accessToken = await GraphApiSettings.GetAccessToken();
            SecureHttpRequest.Setup(s => s.GetAsync(accessToken, accessUri)).ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage("Not found", HttpStatusCode.NotFound));

            var response = await Service.GetGroupByIdAsync(GroupId);

            response.Should().BeNull();
        }

        [Test]
        public void Should_return_user_exception_for_other_responses()
        {
            const string reason = "User not authorised";

            SecureHttpRequest.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(reason, HttpStatusCode.Unauthorized));

            var response = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupByIdAsync(GroupId));

            response.Should().NotBeNull();
            response!.Message.Should().Be($"Failed to get group by id {GroupId}: {reason}");
            response.Reason.Should().Be(reason);
        }
    }
}
