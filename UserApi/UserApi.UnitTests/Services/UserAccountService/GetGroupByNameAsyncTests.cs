using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;

namespace UserApi.UnitTests.Services.UserAccountService;

public class GetGroupByNameAsyncTests : UserAccountServiceTestsBase
{
    private const string GroupName = "testGroup";

    [Test]
    public async Task Should_get_group_by_given_name()
    {
        // Arrange
        var group = new Group { Id = "1", DisplayName = GroupName };
        GraphClient.Setup(x => x.GetGroupByNameAsync(GroupName))
            .ReturnsAsync(group);

        // Act
        var result = await Service.GetGroupByNameAsync(GroupName);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be(GroupName);
        GraphClient.Verify(x => x.GetGroupByNameAsync(GroupName), Times.Once);
    }

    [Test]
    public void Should_throw_UserServiceException_on_ODataError()
    {
        // Arrange
        GraphClient.Setup(x => x.GetGroupByNameAsync(GroupName)).ThrowsAsync(new ODataError());

        // Act & Assert
        var exception = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupByNameAsync(GroupName));
        exception.Should().NotBeNull();
    }

    [Test]
    public void Should_throw_UserServiceException_on_unexpected_error()
    {
        // Arrange
        const string errorMessage = "Unexpected error";
        GraphClient.Setup(x => x.GetGroupByNameAsync(GroupName))
            .ThrowsAsync(new Exception(errorMessage));

        // Act & Assert
        var exception = Assert.ThrowsAsync<UserServiceException>(async () => await Service.GetGroupByNameAsync(GroupName));
        exception.Should().NotBeNull();
        exception!.Message.Should().Be($"An unexpected error occurred while retrieving the group {GroupName}.: {errorMessage}");
    }
}