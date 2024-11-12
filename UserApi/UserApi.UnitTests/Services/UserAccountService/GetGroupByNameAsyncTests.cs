using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph;
using Moq;
using NUnit.Framework;
using Testing.Common.Helpers;
using UserApi.Security;
using UserApi.Services.Models;

namespace UserApi.UnitTests.Services.UserAccountService;

public class GetGroupByNameAsyncTests : UserAccountServiceTests
{
    private const string GroupName = "testGroup";

    [Test]
    public async Task Should_get_group_by_given_name()
    {
        var accessUri = GetAccessUri();
        var group = new Group { DisplayName = GroupName };
        var groupResponse = new GraphQueryResponse<Group>
        {
            Value = [group]
        };

        var accessToken = await GraphApiSettings.GetAccessToken();
        SecureHttpRequest
            .Setup(s => s.GetAsync(accessToken, accessUri))
            .ReturnsAsync(ApiRequestHelper.CreateHttpResponseMessage(groupResponse, HttpStatusCode.OK));

        var response = await Service.GetGroupByNameAsync(GroupName);

        response.Should().NotBeNull();
        response.DisplayName.Should().Be(GroupName);
        SecureHttpRequest.Verify(s => s.GetAsync(accessToken, accessUri), Times.Once);
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
        response!.Message.Should().Be($"Failed to get group by name {GroupName}: {reason}");
        response.Reason.Should().Be(reason);
    }
    
    private string GetAccessUri() => 
        $"{GraphApiSettings.GraphApiBaseUri}v1.0/groups?$filter=displayName eq '{GroupName}'";
}