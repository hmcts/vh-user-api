using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService;

public class GetGroupByIdAsyncTests : UserAccountServiceTestsBase
{
    private const string GroupId = "testId";

    [Test]
    public async Task Should_get_group_by_given_id()
    {
        // Arrange
        var group = new Group { Id = GroupId, DisplayName = "Test Group" };
        GraphClient.Setup(x => x.GetGroupByIdAsync(GroupId))
            .ReturnsAsync(group);

        // Act
        var result = await Service.GetGroupByIdAsync(GroupId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(GroupId);
        result.DisplayName.Should().Be("Test Group");
        GraphClient.Verify(x => x.GetGroupByIdAsync(GroupId), Times.Once);
    }

    [Test]
    public async Task Should_return_null_when_no_matching_group_by_given_id()
    {
        // Arrange
        GraphClient.Setup(x => x.GetGroupByIdAsync(GroupId))
            .ReturnsAsync((Group)null);

        // Act
        var result = await Service.GetGroupByIdAsync(GroupId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Should_return_null_on_ODataError404()
    {
        // Arrange
        GraphClient.Setup(x => x.GetGroupByIdAsync(GroupId))
            .ThrowsAsync(new ODataError{ResponseStatusCode = (int)HttpStatusCode.NotFound});

        // Act & Assert
        Assert.DoesNotThrow(() => Service.GetGroupByIdAsync(GroupId).Result.Should().BeNull());
    }

    [Test]
    public void Should_throw_UserServiceException_on_unexpected_error()
    {
        // Arrange
        GraphClient.Setup(x => x.GetGroupByIdAsync(GroupId))
            .ThrowsAsync(new Exception("error"));

        // Act & Assert
        Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupByIdAsync(GroupId));
    }
}