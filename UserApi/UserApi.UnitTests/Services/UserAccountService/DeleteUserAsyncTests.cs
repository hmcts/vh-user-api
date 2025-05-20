using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using NUnit.Framework;
using UserApi.Security;
using UserApi.Services.Exceptions;

namespace UserApi.UnitTests.Services.UserAccountService;

public class DeleteUserAsyncTests : UserAccountServiceTestsBase
{
    private const string Username = "testUser";

    [Test]
    public async Task Should_delete_user_successfully()
    {
        // Arrange
        GraphClient.Setup(x => x.DeleteUserAsync(Username))
            .Returns(Task.CompletedTask);

        // Act
        await Service.DeleteUserAsync(Username);

        // Assert
        GraphClient.Verify(x => x.DeleteUserAsync(Username), Times.Once);
    }

    [Test]
    public void Should_throw_UserDoesNotExistException_on_ODataError()
    {
        // Arrange
        var odataError = new ODataError { ResponseStatusCode = (int)HttpStatusCode.NotFound };
        GraphClient.Setup(x => x.DeleteUserAsync(Username)).ThrowsAsync(odataError);
        // Act & Assert
        Assert.ThrowsAsync<UserDoesNotExistException>(async () => await Service.DeleteUserAsync(Username));
    }

    [Test]
    public void Should_throw_UserServiceException_on_unexpected_error()
    {
        // Arrange
        const string errorMessage = "Unexpected error";
        GraphClient.Setup(x => x.DeleteUserAsync(Username))
            .ThrowsAsync(new Exception(errorMessage));

        // Act & Assert
        var exception = Assert.ThrowsAsync<UserServiceException>(async () => await Service.DeleteUserAsync(Username));
        exception.Should().NotBeNull();
        exception!.Message.Should().Be($"An unexpected error occurred while deleting the user {Username}.: {errorMessage}");
    }
}